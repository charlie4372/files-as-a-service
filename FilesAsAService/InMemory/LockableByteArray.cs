using System.Threading;

namespace FilesAsAService.InMemory
{
    public class LockableByteArray
    {
        private static readonly byte[] EmptyData = new byte[0];
        
        private readonly Semaphore _lock = new Semaphore(1, 1);

        public void WaitOne()
        {
            _lock.WaitOne();
        }

        public void Release()
        {
            _lock.Release();
        }

        public byte[] Data { get; set; } = EmptyData;
    }
}