using System;

namespace Knapcode.SocketToMe.Sandbox.Dns.ResourceRecords
{
    public class SoaResourceRecord : ResourceRecord
    {
        public string PrimaryNameServer { get; set; }
        public string ResponsibleMailAddress { get; set; }
        public int SerialNumber { get; set; }
        public TimeSpan Refresh { get; set; }
        public TimeSpan Retry { get; set; }
        public TimeSpan Expire { get; set; }
        public TimeSpan Minimum { get; set; }
    }
}
