using System;
using Knapcode.SocketToMe.Sandbox.Dns.Enumerations;
using Knapcode.SocketToMe.Sandbox.Dns.NameRecords;

namespace Knapcode.SocketToMe.Sandbox.Dns
{
    public class RawResourceRecord : Record
    {
        public NameRecord[] Name { get; set; }
        public DnsType Type { get; set; }
        public Class Class { get; set; }
        public TimeSpan Ttl { get; set; }
        public int DataLength { get; set; }
        public int DataStartIndex { get; set; }
    }
}
