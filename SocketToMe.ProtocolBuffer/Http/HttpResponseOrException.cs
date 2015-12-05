using Knapcode.SocketToMe.Http;

namespace Knapcode.SocketToMe.ProtocolBuffer.Http
{
    public class HttpResponseOrException
    {
        public HttpResponse Response { get; set; }
        public string ExceptionString { get; set; }
    }
}