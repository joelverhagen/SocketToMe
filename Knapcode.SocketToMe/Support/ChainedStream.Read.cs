using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Support
{
    public partial class ChainedStream
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            // start the stream enumerator
            if ((!_started && !(_started = _streams.MoveNext())) || _finished)
            {
                return 0;
            }

            // read the streams until the desired amount is returned
            // TODO: support reading > 0 but < count
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

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // start the stream enumerator
            if ((!_started && !(_started = _streams.MoveNext())) || _finished)
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
                        _finished = true;
                        return totalRead;
                    }
                }

                totalRead += read;
            }

            return totalRead;
        }
    }
}