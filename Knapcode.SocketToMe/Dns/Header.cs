using Knapcode.SocketToMe.Dns.Enumerations;

namespace Knapcode.SocketToMe.Dns
{
    public class Header : Record
    {
        public int Id { get; set; }
        public bool IsResponse { get; set; }
        public Opcode Opcode { get; set; }
        public bool IsAuthoritativeAnswer { get; set; }
        public bool IsTruncated { get; set; }
        public bool IsRecursionDesired { get; set; }
        public bool IsRecursionAvailable { get; set; }
        public RequestCode RequestCode { get; set; }
        public ResponseCode ResponseCode { get; set; }
        public int QuestionCount { get; set; }
        public int AnswerCount { get; set; }
        public int NameServerCount { get; set; }
        public int AdditionalRecordCount { get; set; }
    }
}