using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public interface IStore
    {
        Task<Stream> GetAsync(string key, CancellationToken cancellationToken);
        Task SetAsync(string key, Stream stream, CancellationToken cancellationToken);
    }
}