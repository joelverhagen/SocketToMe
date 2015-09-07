using System;
using System.IO;
using Knapcode.SocketToMe.Support;

namespace Knapcode.SocketToMe.Http
{
    public partial class ChunkedStream : Stream
    {
        private readonly ByteStreamReader _byteStreamReader;
        private int _chunkSize = -1;
        private int _remaining = -1;

        public ChunkedStream(Stream innerStream)
        {
            _byteStreamReader = new ByteStreamReader(innerStream, 4096, false);
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
    }
}