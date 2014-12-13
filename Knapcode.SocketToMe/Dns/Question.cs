using Knapcode.SocketToMe.Dns.Enumerations;

namespace Knapcode.SocketToMe.Dns
{
    public class Question : Record
    {
        public string Name { get; set; }
        public QuestionType Type { get; set; }
        public QuestionClass Class { get; set; }
    }
}