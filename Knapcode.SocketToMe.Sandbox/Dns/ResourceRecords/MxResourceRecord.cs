namespace Knapcode.SocketToMe.Sandbox.Dns.ResourceRecords
{
    public class MxResourceRecord : ResourceRecord
    {
        public short Preference { get; set; }
        public string Exchange { get; set; }
    }
}
