using System;
using System.IO;
using System.Threading;

namespace FilesAsAService.InMemory
{
    public class LockableMemoryStream : MemoryStream
    {
        private readonly Semaphore _lock = new Semaphore(1, 1);

        public void WaitOne()
        {
            _lock.WaitOne();
        }

        public void Release()
        {
            _lock.Release();
        }

        public event EventHandler Disposing;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                
            }
            base.Dispose(disposing);
        }
    }
}