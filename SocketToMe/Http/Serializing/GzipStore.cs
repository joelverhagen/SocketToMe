using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Support;

namespace Knapcode.SocketToMe.Http
{
    public class GzipStore : IStore
    {
        private readonly IStore _innerStore;
        private readonly CompressionLevel _compressionLevel;

        public GzipStore(IStore innerStore, CompressionLevel compressionLevel)
        {
            _innerStore = innerStore;
            _compressionLevel = compressionLevel;
        }

        public async Task<Stream> GetAsync(string key, CancellationToken cancellationToken)
        {
            var stream = await _innerStore.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (stream == null)
            {
                return null;
            }

            return new GZipStream(stream, CompressionMode.Decompress);
        }

        public async Task SetAsync(string key, Stream stream, CancellationToken cancellationToken)
        {
            var compressionStream = new InvertedStream(stream, b => new GZipStream(b, _compressionLevel));
            await _innerStore.SetAsync(key, compressionStream, cancellationToken).ConfigureAwait(false);
        }
    }
}
