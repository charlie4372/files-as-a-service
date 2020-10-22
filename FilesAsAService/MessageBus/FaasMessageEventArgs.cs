using System;

namespace FilesAsAService.MessageBus
{
    public class FaasMessageEventArgs : EventArgs
    {
        public Exception? Exception { get; }
        
        public IFaasMessage? Message { get; }
        
        public bool Acknowledged { get; }
        
        public FaasMessageEventArgs(IFaasMessage? message, bool acknowledged, Exception? exception)
        {
            Exception = exception;
            Message = message;
            Acknowledged = acknowledged;
        }
    }
}