using System;
using System.IO;

namespace Knapcode.SocketToMe.Support
{
    public partial class LimitedStream : Stream
    {
        private readonly Stream _innerStream;
        private bool _disposed;
        private long _length;

        public LimitedStream(Stream innerStream, long length)
        {
            _innerStream = innerStream;
            _disposed = false;
            _length = length;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

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
                _innerStream.Dispose();
                _disposed = true;
            }
        }
    }
}