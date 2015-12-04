using System.Net.Http;

namespace Knapcode.SocketToMe.Http.ProtocolBuffer
{
    public class HttpResponseMessageOrException
    {
        public HttpResponseMessage Response { get; set; }
        public string ExceptionString { get; set; }
    }
}