using System;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public partial class ReadsFromChunksStream
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_remaining <= 0)
            {
                var line = _byteStreamReader.ReadLine();
                _chunkSize = (int)Convert.ToUInt32(line, 16);
                _remaining = _chunkSize;
            }

            int read = 0;
            if(_remaining > 0)
            {
                int actualCount = Math.Min(count, _remaining);
                read = _byteStreamReader.Read(buffer, offset, actualCount);
                _remaining -= read;
            }

            if (_remaining == 0)
            {
                _byteStreamReader.ReadLine();
            }

            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_remaining <= 0)
            {
                var line = await _byteStreamReader.ReadLineAsync().ConfigureAwait(false);
                _chunkSize = (int)Convert.ToUInt32(line, 16);
                _remaining = _chunkSize;
            }

            int read = 0;
            if(_remaining > 0)
            {
                int actualCount = Math.Min(count, _remaining);
                read = await _byteStreamReader.ReadAsync(buffer, offset, actualCount).ConfigureAwait(false);
                _remaining -= read;
            }

            if (_remaining == 0)
            {
                await _byteStreamReader.ReadLineAsync().ConfigureAwait(false);
            }

            return read;
        }
    }
}