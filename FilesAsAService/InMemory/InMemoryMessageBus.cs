using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FilesAsAService.MessageBus;

namespace FilesAsAService.InMemory
{
    public class InMemoryMessageBus : IFaasMessageBus
    {
        public event FaasMessageEventHandler? MessageProcessed;
        
        private object _syncRoot = new object();

        private bool _isRunning = false;
        
        private readonly Queue<MessageAndAction> _actions = new Queue<MessageAndAction>();
        
        private readonly FaasMessageProcessor _messageProcessor;

        public InMemoryMessageBus()
        {
            _messageProcessor = new FaasMessageProcessor();
        }

        /// <inheritdoc cref="AddContainer"/>
        public void AddContainer(FaasContainer container)
        {
            _messageProcessor.AddContainer(container);
        }

        public Task Send(FaasDeleteFromStoreMessageV1 fromStoreMessage)
        {
            lock (_actions)
            {
                _actions.Enqueue(new MessageAndAction
                {
                    Message = fromStoreMessage,
                    Action = () => _messageProcessor.Process(fromStoreMessage, CancellationToken.None)
                });
            }
            
            RunNext();

            return Task.CompletedTask;
        }

        private void RunNext()
        {
            if (_isRunning)
                return;

            MessageAndAction task;
            lock (_syncRoot)
            {
                if (_isRunning || _actions.Count == 0)
                    return;

                task = _actions.Dequeue();

                _isRunning = true;
            }

            // TODO this will fail if the thread pool is exhausted. How much use is this going to get outside of tests...
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    task.Action.Invoke().Wait();
                    
                    OnMessageProcessed(task.Message, true, null);
                }
                catch (Exception ex)
                {
                    OnMessageProcessed(task.Message, false, ex);
                }

                lock (_syncRoot)
                {
                    _isRunning = false;
                }
                
                RunNext();
            });
        }

        private void OnMessageProcessed(IFaasMessage? message, bool acknowledged, Exception? exception)
        {
            FaasMessageEventHandler? handler = MessageProcessed;
            handler?.Invoke(this, new FaasMessageEventArgs(message, acknowledged, exception));
        }

        private class MessageAndAction
        {
            public IFaasMessage Message;

            public Func<Task> Action;
        }
    }
}