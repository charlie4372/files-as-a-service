using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FilesAsAService.MessageBus.RabbitMQ
{
    public class FaasRabbitMqMessageBus : IFaasMessageBus, IDisposable
    {
        /// <summary>
        /// The name of the delete from store queue.
        /// </summary>
        private const string DeleteStoreQueue = "faas-store-delete-queue";

        /// <summary>
        /// The channel pool.
        /// </summary>
        private readonly FaasRabbitMqChannelPool _channelPool;

        /// <summary>
        /// The consumer channel.
        /// </summary>
        private IModel? _consumerChannel;
        
        /// <summary>
        /// The message processor.
        /// </summary>
        private readonly FaasMessageProcessor _messageProcessor;

        /// <inheritdoc cref="MessageProcessed"/>
        public event FaasMessageEventHandler? MessageProcessed;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public FaasRabbitMqMessageBus(FaasRabbitMqOptions options)
        {
            _messageProcessor = new FaasMessageProcessor();
            
            if (options == null) throw new ArgumentNullException(nameof(options));

            // Create the connection factory.
            var factory = new ConnectionFactory
            {
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true
            };
            if (options.HostName != null)
                factory.HostName = options.HostName;
            if (options.UserName != null)
                factory.UserName = options.UserName;
            if (options.Password != null)
                factory.Password = options.Password;
            if (options.VirtualHost != null)
                factory.VirtualHost = options.VirtualHost;
            
            // Create the pool.
            _channelPool = new FaasRabbitMqChannelPool(factory);
            
            CreateAllQueues();
            CreateConsumer();
        }

        /// <inheritdoc cref="AddContainer"/>
        public void AddContainer(FaasContainer container)
        {
            _messageProcessor.AddContainer(container);
            container.SetMessageBus(this);
        }

        /// <summary>
        /// Creates all of the queues needed.
        /// </summary>
        private void CreateAllQueues()
        {
            var channel = _channelPool.Rent();
            try
            {
                channel.QueueDeclare(DeleteStoreQueue, true, false, false, null);
            }
            finally
            {
                _channelPool.Return(channel);
            }
        }

        /// <summary>
        /// Creates the consumer.
        /// </summary>
        /// <exception cref="InvalidOperationException">If there is already a consumer created.</exception>
        private void CreateConsumer()
        {
            if (_consumerChannel != null)
                throw new InvalidOperationException("Consumer channel already exists.");

            // Rent the channel.
            _consumerChannel = _channelPool.Rent();
            _consumerChannel.BasicQos(0, 1, false);

            // Register the message and bind it to the processor.
            RegisterConsumer<FaasDeleteFromStoreMessageV1>(_consumerChannel, 
                DeleteStoreQueue,
                (message, cancellationToken) => _messageProcessor.Process(message, cancellationToken));
        }

        /// <summary>
        /// Registers a consumer for processing messages.
        /// </summary>
        /// <param name="channel">The channel to listen on.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="action">The action to run.</param>
        /// <typeparam name="TMessage">The message type.</typeparam>
        private void RegisterConsumer<TMessage>(IModel channel, string routingKey, Func<TMessage, CancellationToken, Task> action)
            where TMessage : IFaasMessage 
        {
            channel.QueueDeclare(DeleteStoreQueue, true, false, false, null);
            channel.QueueDeclare(DeleteStoreQueue + "-completed", true, false, false, null);

            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            // Define the receive delegate.
            consumer.Received += async (sender, ea) =>
            {
                var message = ParseJson<TMessage>(ea);
                try
                {
                    var cancellationToken = new CancellationToken();
                    if (ea.RoutingKey == routingKey)
                    {
                        await action(message, cancellationToken);
                        Acknowledge(ea);
                        OnMessageProcessed(message, true, null);
                    }
                    else
                    {
                        Reject(ea);
                        OnMessageProcessed(message, false, null);
                    }
                }
                catch (Exception exception)
                {
                    // TODO logging or something
                    OnMessageProcessed(message, false, exception);
                }
            };
            // Add the consumer to the channel.
            _consumerChannel.BasicConsume(
                DeleteStoreQueue,
                false,
                consumer);
        }

        /// <inheritdoc cref="Send(FaasDeleteFromStoreMessageV1)"/>
        public Task Send(FaasDeleteFromStoreMessageV1 fromStoreMessage)
        {
            var channel = _channelPool.Rent();
            try
            {
                SendJson(channel, DeleteStoreQueue, fromStoreMessage);
            }
            finally
            {
                _channelPool.Return(channel);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Acknowledges a message.
        /// </summary>
        /// <param name="args">The event args.</param>
        private void Acknowledge(BasicDeliverEventArgs args)
        {
            var channel = _channelPool.Rent();
            try
            {
                channel.BasicAck(args.DeliveryTag, false);
            }
            finally
            {
                _channelPool.Return(channel);
            }
        }
        
        /// <summary>
        /// Rejects a message.
        /// </summary>
        /// <param name="args">The event args.</param>
        private void Reject(BasicDeliverEventArgs args)
        {
            var channel = _channelPool.Rent();
            try
            {
                channel.BasicReject(args.DeliveryTag, false);
            }
            finally
            {
                _channelPool.Return(channel);
            }
        }

        /// <summary>
        /// Sends a message, as JSON, to a channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="message">The message.</param>
        private void SendJson(IModel channel, string routingKey, object message)
        {
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentEncoding = "UTF-8";
            properties.ContentType = "application/json";
            
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            channel.BasicPublish(exchange: "",
                routingKey: routingKey,
                basicProperties: properties,
                body: body);
        }

        /// <summary>
        /// Reads a JSON response from the delivery args.
        /// </summary>
        /// <param name="args">The delivery args.</param>
        /// <typeparam name="TValue">The message.</typeparam>
        /// <returns>The message.</returns>
        private TValue ParseJson<TValue>(BasicDeliverEventArgs args)
        {
            var bodyString = Encoding.UTF8.GetString(args.Body.ToArray());
            return JsonSerializer.Deserialize<TValue>(bodyString);
        }

        /// <summary>
        /// Raises the MessageProcessed event.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="acknowledged">If the message is acknowledged.</param>
        /// <param name="exception">Any exception that may have occured.</param>
        private void OnMessageProcessed(IFaasMessage? message, bool acknowledged, Exception? exception)
        {
            FaasMessageEventHandler? handler = MessageProcessed;
            handler?.Invoke(this, new FaasMessageEventArgs(message, acknowledged, exception));
        }

        /// <inheritdoc cref="Dispose"/>
        public void Dispose()
        {
            if (_consumerChannel != null)
                _channelPool.Return(_consumerChannel);
            
            _channelPool.Dispose();
            
            GC.SuppressFinalize(this);
        }
    }
}