using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Support
{
    public class ChainedStream : Stream
    {
        private readonly bool _disposeOnCompletion;
        private readonly IEnumerator<Stream> _streams;
        private bool _finished;
        private bool _started;

        public ChainedStream(IEnumerable<Stream> streams, bool disposeOnCompletion = true)
        {
            _started = false;
            _finished = false;
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

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // start the stream enumerator
            if (!_started && !(_started = _streams.MoveNext()))
            {
                return 0;
            }

            // read the streams until the desired amount is returned
            // TODO: support reading > 0 but < count
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = await _streams.Current.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken);
                if (read == 0)
                {
                    if (_disposeOnCompletion)
                    {
                        _streams.Current.Dispose();
                    }

                    if (!_streams.MoveNext())
                    {
                        return totalRead;
                    }
                }

                totalRead += read;
            }

            return totalRead;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // start the stream enumerator
            if ((!_started && !(_started = _streams.MoveNext())) || _finished)
            {
                return 0;
            }

            // read the streams until the desired amount is returned
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = _streams.Current.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                {
                    if (_disposeOnCompletion)
                    {
                        _streams.Current.Dispose();
                    }

                    if (!_streams.MoveNext())
                    {
                        _finished = true;
                        return totalRead;
                    }
                }

                totalRead += read;
            }

            return totalRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}