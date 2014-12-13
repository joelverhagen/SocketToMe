using Knapcode.SocketToMe.Dns.Enumerations;

namespace Knapcode.SocketToMe.Dns.ResourceRecords
{
    public class DnskeyResourceRecord : ResourceRecord
    {
        public bool HoldsDnsZoneKey { get; set; }
        public bool HoldsKeyForSecureEntryPoint { get; set; }
        public byte Protocol { get; set; }
        public DnssecAlgorithmType Algorithm { get; set; }
        public byte[] PublicKey { get; set; }
    }
}
