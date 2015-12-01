using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public class FileSystemStore : IStore
    {
        private const int BufferSize = 4096;
        private readonly string _directory;

        public FileSystemStore(string directory)
        {
            _directory = directory;
        }

        public Task<Stream> GetAsync(string key, CancellationToken cancellationToken)
        {
            var path = GetPath(key);
            try
            {
                var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);
                return Task.FromResult((Stream) fileStream);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        public async Task SetAsync(string key, Stream stream, CancellationToken cancellationToken)
        {
            var path = GetPath(key);
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write, BufferSize, FileOptions.Asynchronous))
            {
                await stream.CopyToAsync(fileStream, BufferSize, cancellationToken);
            }
        }

        private string GetPath(string key)
        {
            return Path.Combine(_directory, key);
        }
    }
}
