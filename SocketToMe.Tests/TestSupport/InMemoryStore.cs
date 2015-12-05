using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Http;

namespace Knapcode.SocketToMe.Tests.TestSupport
{
    public class InMemoryStore : IStore
    {
        private readonly IDictionary<string, byte[]> _blobs = new Dictionary<string, byte[]>();

        public Task<Stream> GetAsync(string key, CancellationToken cancellationToken)
        {
            byte[] bytes;
            if (!_blobs.TryGetValue(key, out bytes))
            {
                return Task.FromResult((Stream) null);
            }

            var memoryStream = new MemoryStream(bytes);
            return Task.FromResult((Stream) memoryStream);
        }

        public async Task SetAsync(string key, Stream stream, CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            _blobs[key] = memoryStream.ToArray();
        }
    }
}
