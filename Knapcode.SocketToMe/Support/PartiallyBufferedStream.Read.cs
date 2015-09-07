using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Support
{
    public partial class PartiallyBufferedStream
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            int read;
            if (TryReadBuffer(buffer, offset, count, out read))
            {
                return read;
            }

            return _innerStream.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read;
            if (TryReadBuffer(buffer, offset, count, out read))
            {
                return read;
            }

            return await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }
}