using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Dns.Enumerations;

namespace Knapcode.SocketToMe.Dns.ResourceRecords
{
    public class DsResourceRecord : ResourceRecord
    {
        public short KeyTag { get; set; }
        public DnssecAlgorithmType Algorithm { get; set; }
        public DnssecDigestType DigestType { get; set; }
        public byte[] Digest { get; set; }
    }
}
