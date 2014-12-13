using System;
using Knapcode.SocketToMe.Dns.Enumerations;

namespace Knapcode.SocketToMe.Dns.ResourceRecords
{
    public abstract class ResourceRecord
    {
        public string Name { get; set; }
        public DnsType Type { get; set; }
        public Class Class { get; set; }
        public TimeSpan Ttl { get; set; }
    }
}
