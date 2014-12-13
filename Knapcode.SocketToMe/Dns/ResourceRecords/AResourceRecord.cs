using System.Net;

namespace Knapcode.SocketToMe.Dns.ResourceRecords
{
    public class AResourceRecord : ResourceRecord
    {
        public IPAddress Address { get; set; }
    }
}
