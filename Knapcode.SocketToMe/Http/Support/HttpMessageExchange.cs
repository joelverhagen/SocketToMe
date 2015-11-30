using System.Net.Http;

namespace Knapcode.SocketToMe.Http
{
    public class HttpMessageExchange
    {
        public HttpRequestMessage Request { get; set; }
        public HttpResponseMessage Response { get; set; }
    }
}
