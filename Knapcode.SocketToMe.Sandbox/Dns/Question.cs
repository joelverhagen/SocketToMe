using Knapcode.SocketToMe.Sandbox.Dns.Enumerations;

namespace Knapcode.SocketToMe.Sandbox.Dns
{
    public class Question : Record
    {
        public string Name { get; set; }
        public QuestionType Type { get; set; }
        public QuestionClass Class { get; set; }
    }
}