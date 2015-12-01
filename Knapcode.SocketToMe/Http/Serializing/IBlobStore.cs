using System.IO;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public interface IBlobStore
    {
        Task<Stream> GetAsync(string key);
        Task SetAsync(string key, Stream stream);
    }
}