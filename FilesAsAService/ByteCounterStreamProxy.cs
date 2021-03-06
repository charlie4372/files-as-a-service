using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService
{
    /// <summary>
    /// Counts the number of bytes read from the input stream as it is read.
    /// </summary>
    public class ByteCounterStreamProxy : Stream
    {
        /// <summary>
        /// The stream to proxy.
        /// </summary>
        private readonly Stream _proxiedStream;

        /// <summary>
        /// Determines whether the proxied stream should be closed.
        /// </summary>
        private readonly bool _closeProxiedStream;

        /// <summary>
        /// The total number of bytes read.
        /// </summary>
        public long TotalBytesRead { get; private set; }

        public ByteCounterStreamProxy(Stream stream, bool closeProxiedStream)
        {
            _proxiedStream = stream ?? throw new ArgumentNullException(nameof(closeProxiedStream));
            _closeProxiedStream = closeProxiedStream;
            
            if (!stream.CanRead) throw new ArgumentException("Stream does not support reading.", nameof(stream));
        }

        /// <inheritdoc cref="Flush"/>
        public override void Flush()
        {
            _proxiedStream.Flush();
        }

        /// <inheritdoc cref="Read(byte[], int, int)"/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = _proxiedStream.Read(buffer, offset, count);
            TotalBytesRead += bytesRead;
            return bytesRead;
        }

        /// <inheritdoc cref="Seek"/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="SetLength"/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="Write(byte[],int,int)"/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="CanRead"/>
        public override bool CanRead => true;
        
        /// <inheritdoc cref="CanSeek"/>
        public override bool CanSeek => false;
        
        /// <inheritdoc cref="CanWrite"/>
        public override bool CanWrite => false;
        
        /// <inheritdoc cref="Length"/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc cref="Position"/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc cref="Close"/>
        public override void Close()
        {
            if (_closeProxiedStream)
                _proxiedStream.Close();
        }

        /// <inheritdoc cref="Read(Span&lt;byte&gt;)"/>
        public override int Read(Span<byte> buffer)
        {
            var bytesRead = _proxiedStream.Read(buffer);
            TotalBytesRead += bytesRead;
            return bytesRead;
        }

        /// <inheritdoc cref="Write(ReadOnlySpan&lt;byte&gt;)"/>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="BeginRead"/>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object? state)
        {
            return _proxiedStream.BeginRead(buffer, offset, count, callback, state);
        }

        /// <inheritdoc cref="BeginWrite"/>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object? state)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="CanTimeout"/>
        public override bool CanTimeout => _proxiedStream.CanTimeout;

        /// <inheritdoc cref="CopyTo"/>
        public override void CopyTo(Stream destination, int bufferSize)
        {
            _proxiedStream.CopyTo(destination, bufferSize);
        }

        /// <inheritdoc cref="DisposeAsync"/>
        public override ValueTask DisposeAsync()
        {
            return _proxiedStream.DisposeAsync();
        }

        /// <inheritdoc cref="EndRead"/>
        public override int EndRead(IAsyncResult asyncResult)
        {
            var bytesRead = _proxiedStream.EndRead(asyncResult);
            TotalBytesRead = bytesRead;
            return bytesRead;
        }

        /// <inheritdoc cref="EndWrite"/>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="FlushAsync"/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _proxiedStream.FlushAsync(cancellationToken);
        }

        /// <inheritdoc cref="ReadAsync(byte[], int, int, CancellationToken)"/>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await _proxiedStream.ReadAsync(buffer, offset, count, cancellationToken);
            TotalBytesRead += bytesRead;
            return bytesRead;
        }

        /// <inheritdoc cref="ReadAsync(Memory&lt;byte&gt;, CancellationToken)"/>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var bytesRead = await _proxiedStream.ReadAsync(buffer, cancellationToken);
            TotalBytesRead += bytesRead;
            return bytesRead;
        }

        /// <inheritdoc cref="ReadByte"/>
        public override int ReadByte()
        {
            var result = _proxiedStream.ReadByte();
            TotalBytesRead++;
            return result;
        }

        /// <inheritdoc cref="ReadTimeout"/>
        public override int ReadTimeout
        {
            get => _proxiedStream.ReadTimeout;
            set => _proxiedStream.ReadTimeout = value;
        }

        /// <inheritdoc cref="WriteAsync(byte[], int, int, CancellationToken)"/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="WriteAsync(ReadOnlyMemory&lt;byte&gt;, CancellationToken)"/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="WriteByte"/>
        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="WriteTimeout"/>
        public override int WriteTimeout
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc cref="CopyToAsync"/>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _proxiedStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        /// <inheritdoc cref="InitializeLifetimeService"/>
        public override object InitializeLifetimeService()
        {
            return _proxiedStream.InitializeLifetimeService();
        }

        /// <inheritdoc cref="Dispose"/>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _closeProxiedStream)
                _proxiedStream.Dispose();
            
            base.Dispose(disposing);
        }
    }
}