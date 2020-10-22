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
        private const string DeleteFileQueue = "faas-file-delete-queue";
        private const string DeleteFileQueueCompleted = "faas-file-delete-queue-completed";

        private readonly FaasRabbitMqChannelPool _channelPool;

        private readonly IFaasMessageProcessor _messageProcessor;

        private IModel? _consumerChannel;

        public event FaasMessageEventHandler? MessageProcessed;

        public FaasRabbitMqMessageBus(FaasRabbitMqOptions options, IFaasMessageProcessor messageProcessor)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));

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
            
            _channelPool = new FaasRabbitMqChannelPool(factory);
            
            CreateAllQueues();
            CreateConsumer();
        }

        private void CreateAllQueues()
        {
            var channel = _channelPool.Rent();
            try
            {
                channel.QueueDeclare(DeleteFileQueue, true, false, false, null);
            }
            finally
            {
                _channelPool.Return(channel);
            }
        }

        private void CreateConsumer()
        {
            if (_consumerChannel != null)
                throw new InvalidOperationException("Consumer channel already exists.");

            _consumerChannel = _channelPool.Rent();
            _consumerChannel.BasicQos(0, 1, false);

            RegisterConsumer<FaasDeleteFileVersionMessageV1>(_consumerChannel, 
                DeleteFileQueue,
                (message, cancellationToken) => _messageProcessor.Process(message, cancellationToken));
        }

        private void RegisterConsumer<TMessage>(IModel channel, string routingKey, Func<TMessage, CancellationToken, Task> action)
            where TMessage : IFaasMessage 
        {
            channel.QueueDeclare(DeleteFileQueue, true, false, false, null);
            channel.QueueDeclare(DeleteFileQueue + "-completed", true, false, false, null);

            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
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
            _consumerChannel.BasicConsume(
                DeleteFileQueue,
                false,
                consumer);
        }

        public Task Send(FaasDeleteFileVersionMessageV1 versionMessage)
        {
            var channel = _channelPool.Rent();
            try
            {
                SendJson(channel, DeleteFileQueue, versionMessage);
            }
            finally
            {
                _channelPool.Return(channel);
            }

            return Task.CompletedTask;
        }

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

        private TValue ParseJson<TValue>(BasicDeliverEventArgs args)
        {
            var bodyString = Encoding.UTF8.GetString(args.Body.ToArray());
            return JsonSerializer.Deserialize<TValue>(bodyString);
        }

        private void OnMessageProcessed(IFaasMessage? message, bool acknowledged, Exception? exception)
        {
            FaasMessageEventHandler? handler = MessageProcessed;
            handler?.Invoke(this, new FaasMessageEventArgs(message, acknowledged, exception));
        }

        public void Dispose()
        {
            if (_consumerChannel != null)
                _channelPool.Return(_consumerChannel);
            
            _channelPool.Dispose();
            
            GC.SuppressFinalize(this);
        }
    }
}