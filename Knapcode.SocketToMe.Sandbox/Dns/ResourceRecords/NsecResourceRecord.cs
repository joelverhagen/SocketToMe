using System.Collections.Generic;
using Knapcode.SocketToMe.Sandbox.Dns.Enumerations;

namespace Knapcode.SocketToMe.Sandbox.Dns.ResourceRecords
{
    public class NsecResourceRecord : ResourceRecord
    {
        public string NextDomainName { get; set; }
        public IEnumerable<DnsType> AllTypes { get; set; }
    }
}
