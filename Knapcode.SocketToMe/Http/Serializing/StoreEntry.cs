using System.IO;

namespace Knapcode.SocketToMe.Http
{
    public class StoreEntry
    {
        public string Key { get; set; }
        public Stream Stream { get; set; }
    }
}