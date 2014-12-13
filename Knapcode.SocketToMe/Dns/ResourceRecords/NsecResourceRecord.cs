using System.Collections.Generic;
using Knapcode.SocketToMe.Dns.Enumerations;

namespace Knapcode.SocketToMe.Dns.ResourceRecords
{
    public class NsecResourceRecord : ResourceRecord
    {
        public string NextDomainName { get; set; }
        public IEnumerable<DnsType> AllTypes { get; set; }
    }
}
