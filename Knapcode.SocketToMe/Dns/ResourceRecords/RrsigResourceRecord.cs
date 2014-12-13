using System;
using Knapcode.SocketToMe.Dns.Enumerations;

namespace Knapcode.SocketToMe.Dns.ResourceRecords
{
    public class RrsigResourceRecord : ResourceRecord
    {
        public DnsType TypeCovered { get; set; }
        public DnssecAlgorithmType Algorithm { get; set; }
        public int LabelCount { get; set; }
        public TimeSpan OriginalTtl { get; set; }
        public DateTime SignatureExpiration { get; set; }
        public DateTime SignatureInception { get; set; }
        public short KeyTag { get; set; }
        public string SignerName { get; set; }
        public byte[] Signature { get; set; }
    }
}