namespace Knapcode.SocketToMe.Dns.ResourceRecords
{
    public class MxResourceRecord : ResourceRecord
    {
        public short Preference { get; set; }
        public string Exchange { get; set; }
    }
}
