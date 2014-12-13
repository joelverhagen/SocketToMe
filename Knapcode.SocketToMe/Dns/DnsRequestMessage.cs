using System.Collections.Generic;

namespace Knapcode.SocketToMe.Dns
{
    public class DnsRequestMessage
    {
        public IEnumerable<Question> Questions { get; set; }
    }
}
