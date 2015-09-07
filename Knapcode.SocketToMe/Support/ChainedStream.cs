using System;
using System.Collections.Generic;
using System.IO;

namespace Knapcode.SocketToMe.Support
{
    public partial class ChainedStream : Stream
    {
        private readonly bool _disposeOnCompletion;
        private readonly IEnumerator<Stream> _streams;
        private bool _disposed;
        private bool _finished;
        private bool _started;

        public ChainedStream(IEnumerable<Stream> streams, bool disposeOnCompletion = true)
        {
            _started = false;
            _finished = false;
            _disposed = false;
            _streams = streams.GetEnumerator();
            _disposeOnCompletion = disposeOnCompletion;
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
            if (!_disposed)
            {
                if (!_started && !_streams.MoveNext())
                {
                    _disposed = true;
                    return;
                }

                do
                {
                    _streams.Current.Dispose();
                } while (_streams.MoveNext());
                _disposed = true;
            }
        }
    }
}