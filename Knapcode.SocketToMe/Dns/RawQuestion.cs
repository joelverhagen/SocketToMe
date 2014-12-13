using Knapcode.SocketToMe.Dns.Enumerations;
using Knapcode.SocketToMe.Dns.NameRecords;

namespace Knapcode.SocketToMe.Dns
{
    public class RawQuestion : Record
    {
        public NameRecord[] Name { get; set; }
        public QuestionType Type { get; set; }
        public QuestionClass Class { get; set; }
    }
}