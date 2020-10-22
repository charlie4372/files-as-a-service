using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FilesAsAService.MessageBus;

namespace FilesAsAService.InMemory
{
    public class InMemoryFaasMessageBus : IFaasMessageBus
    {
        private readonly IFaasMessageProcessor _messageProcessor;
        
        private readonly Queue<IFaasMessage> _queue = new Queue<IFaasMessage>();
        
        private readonly object _syncRoot = new object();

        private bool _running;

        public event FaasMessageEventHandler MessageProcessed;
        
        public InMemoryFaasMessageBus(IFaasMessageProcessor messageProcessor)
        {
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
        }

        public Task Send(FaasDeleteFileVersionMessageV1 versionMessage)
        {
            lock (_syncRoot)
            {
                _queue.Enqueue(versionMessage);
            }
            
            RunNext();
            
            return Task.CompletedTask;
        }

        private void RunNext()
        {
            if (_running)
                return;

            IFaasMessage? message;
            lock (_syncRoot)
            {
                if (_running)
                    return;
                
                message = Dequeue();
                if (message == null)
                    return;

                _running = true;
            }

            Task.Run(async () =>
            {
                try
                {
                    if (message is FaasDeleteFileVersionMessageV1 deleteFileVersionMessageV1)
                    {
                        await _messageProcessor.Process(deleteFileVersionMessageV1, CancellationToken.None);
                        OnMessageProcessed(deleteFileVersionMessageV1, true, null);
                    }
                    else
                    {
                        OnMessageProcessed(message, false, null);
                    }
                }
                catch (Exception exception)
                {
                    // TODO add logging or something
                    OnMessageProcessed(message, true, exception);
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _running = false;
                    }
                }
                
                RunNext();
            });
        }

        private void OnMessageProcessed(IFaasMessage? message, bool acknowledged, Exception? exception)
        {
            FaasMessageEventHandler? handler = MessageProcessed;
            handler?.Invoke(this, new FaasMessageEventArgs(message, acknowledged, exception));
        }

        private IFaasMessage? Dequeue()
        {
            if (_queue.Count == 0)
                return null;

            return _queue.Dequeue();
        }
    }
}