using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Http;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Knapcode.SocketToMe.AzureStorage.Http
{
    public class SimpleAzureBlobStore : IStore
    {
        private readonly CloudBlobContainer _container;

        public SimpleAzureBlobStore(CloudBlobContainer container)
        {
            _container = container;
        }

        public async Task<Stream> GetAsync(string key, CancellationToken cancellationToken)
        {
            var reference = GetBlockBlobReference(key);
            var exists = await reference.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                return null;
            }

            return await reference.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        }

        private CloudBlockBlob GetBlockBlobReference(string key)
        {
            return _container.GetBlockBlobReference(key);
        }

        public async Task SetAsync(string key, Stream stream, CancellationToken cancellationToken)
        {
            var reference = GetBlockBlobReference(key);
            await reference.UploadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }
}
