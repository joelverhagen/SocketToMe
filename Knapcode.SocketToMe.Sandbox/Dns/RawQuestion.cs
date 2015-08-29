using Knapcode.SocketToMe.Sandbox.Dns.Enumerations;
using Knapcode.SocketToMe.Sandbox.Dns.NameRecords;

namespace Knapcode.SocketToMe.Sandbox.Dns
{
    public class RawQuestion : Record
    {
        public NameRecord[] Name { get; set; }
        public QuestionType Type { get; set; }
        public QuestionClass Class { get; set; }
    }
}