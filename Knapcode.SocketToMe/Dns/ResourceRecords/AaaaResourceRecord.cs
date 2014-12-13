using System.Net;

namespace Knapcode.SocketToMe.Dns.ResourceRecords
{
    public class AaaaResourceRecord : ResourceRecord
    {
        public IPAddress Address { get; set; }
    }
}
