using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Knapcode.SocketToMe.Tests.TestSupport
{
    public class MapStream : Stream
    {
        private readonly Stream _destination;
        private readonly Func<byte[], IEnumerable<byte>> _map;

        public MapStream(Stream destination, Func<byte[], IEnumerable<byte>> map)
        {
            _destination = destination;
            _map = map;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

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

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var originalBytes = buffer.Skip(offset).Take(count).ToArray();
            var mappedBytes = _map(originalBytes).ToArray();
            _destination.Write(mappedBytes, 0, mappedBytes.Length);
        }
    }
}
