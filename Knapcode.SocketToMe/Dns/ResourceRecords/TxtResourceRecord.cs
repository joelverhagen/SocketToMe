using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Dns.ResourceRecords
{
    public class TxtResourceRecord : ResourceRecord
    {
        public string Text { get; set; }
        public byte[] Data { get; set; }
    }
}
