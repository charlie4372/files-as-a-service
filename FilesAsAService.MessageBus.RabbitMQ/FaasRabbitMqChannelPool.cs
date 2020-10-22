using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace FilesAsAService.MessageBus.RabbitMQ
{
    public class FaasRabbitMqChannelPool : IDisposable
    {
        private readonly IConnection _connection;

        private readonly Queue<IModel> _channelQueue;
        
        private readonly object _syncRoot = new object();

        public FaasRabbitMqChannelPool(ConnectionFactory connectionFactory)
        {
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));
            _connection = connectionFactory.CreateConnection();
            
            _channelQueue = new Queue<IModel>();
        }

        public IModel Rent()
        {
            lock (_syncRoot)
            {
                return _channelQueue.Count > 0 ? _channelQueue.Dequeue() : _connection.CreateModel();
            }
        }

        public void Return(IModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            
            // TODO check the model is still good, if not dispose of it and don't add it.
            
            lock (_syncRoot)
            {
                _channelQueue.Enqueue(model);
            }
        }

        public void Dispose()
        {
            while (_channelQueue.Count > 0)
                _channelQueue.Dequeue().Dispose();
            
            _connection.Dispose();
            
            GC.SuppressFinalize(this);
        }
    }
}