using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService
{
    public class FaasWrappedStream : Stream
    {
        private readonly Stream _baseStream;

        public EventHandler Disposing;
        
        public FaasWrappedStream(Stream stream)
        {
            _baseStream = stream;
        }

        public override void Flush()
        {
            _baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override void Close()
        {
            _baseStream.Close();
        }

        public override int Read(Span<byte> buffer)
        {
            return _baseStream.Read(buffer);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _baseStream.Write(buffer);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object? state)
        {
            return _baseStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object? state)
        {
            return _baseStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override bool CanTimeout => _baseStream.CanTimeout;

        public override void CopyTo(Stream destination, int bufferSize)
        {
            _baseStream.CopyTo(destination, bufferSize);
        }

        public override ValueTask DisposeAsync()
        {
            return _baseStream.DisposeAsync();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _baseStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _baseStream.EndWrite(asyncResult);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _baseStream.FlushAsync(cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return _baseStream.ReadAsync(buffer, cancellationToken);
        }

        public override int ReadByte()
        {
            return _baseStream.ReadByte();
        }

        public override int ReadTimeout
        {
            get => _baseStream.ReadTimeout;
            set => _baseStream.ReadTimeout = value;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return _baseStream.WriteAsync(buffer, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            _baseStream.WriteByte(value);
        }

        public override int WriteTimeout { get => _baseStream.WriteTimeout;
            set => _baseStream.WriteTimeout = value;
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _baseStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override object InitializeLifetimeService()
        {
            return _baseStream.InitializeLifetimeService();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                OnDisposing();
                
                _baseStream.Dispose();
            }

            base.Dispose(disposing);
        }

        protected void OnDisposing()
        {
            Disposing(this, EventArgs.Empty);
        }
    }
}