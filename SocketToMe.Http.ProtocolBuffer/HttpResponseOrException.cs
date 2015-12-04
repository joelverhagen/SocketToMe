namespace Knapcode.SocketToMe.Http.ProtocolBuffer
{
    public class HttpResponseOrException
    {
        public HttpResponse Response { get; set; }
        public string ExceptionString { get; set; }
    }
}