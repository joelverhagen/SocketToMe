using System.Collections.Generic;
using Knapcode.SocketToMe.Sandbox.Dns.ResourceRecords;

namespace Knapcode.SocketToMe.Sandbox.Dns
{
    public class DnsResponseMessage
    {
        public byte[] ResponseContent { get; set; }
        public Header Header { get; set; }
        public IEnumerable<Question> Questions { get; set; }
        public IEnumerable<ResourceRecord> Answers { get; set; }
        public IEnumerable<ResourceRecord> NameServers { get; set; }
        public IEnumerable<ResourceRecord> AdditionalRecords { get; set; }
    }
}