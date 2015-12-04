using System;
using System.IO;
using Knapcode.SocketToMe.Support;

namespace Knapcode.SocketToMe.Http
{
    public partial class ChunkedStream : Stream
    {
        private readonly ByteStreamReader _byteStreamReader;
        private bool _disposed;
        private int _chunkSize;
        private int _remaining;

        public ChunkedStream(Stream innerStream)
        {
            _byteStreamReader = new ByteStreamReader(innerStream, 4096, false);
            _disposed = false;
            _chunkSize = -1;
            _remaining = -1;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _byteStreamReader.Dispose();
                _disposed = true;
            }
        }
    }
}