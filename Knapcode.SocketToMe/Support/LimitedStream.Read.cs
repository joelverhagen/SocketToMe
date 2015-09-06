using System;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Support
{
    public partial class LimitedStream
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            var limitedCount = (int) Math.Min(_length, count);
            if (limitedCount == 0)
            {
                return 0;
            }

            var read = _innerStream.Read(buffer, offset, limitedCount);
            _length -= read;
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var limitedCount = (int) Math.Min(_length, count);
            if (limitedCount == 0)
            {
                return 0;
            }

            var read = await _innerStream.ReadAsync(buffer, offset, limitedCount, cancellationToken);
            _length -= read;
            return read;
        }
	}
}