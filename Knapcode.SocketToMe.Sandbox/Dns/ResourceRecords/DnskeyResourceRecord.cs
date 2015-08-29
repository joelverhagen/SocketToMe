using Knapcode.SocketToMe.Sandbox.Dns.Enumerations;

namespace Knapcode.SocketToMe.Sandbox.Dns.ResourceRecords
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
