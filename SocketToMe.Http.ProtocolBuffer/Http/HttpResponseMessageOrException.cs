using System.Net.Http;

namespace Knapcode.SocketToMe.ProtocolBuffer.Http
{
    public class HttpResponseMessageOrException
    {
        public HttpResponseMessage Response { get; set; }
        public string ExceptionString { get; set; }
    }
}