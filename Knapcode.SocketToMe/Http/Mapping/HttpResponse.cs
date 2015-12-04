using System.Collections.Generic;
using System.IO;

namespace Knapcode.SocketToMe.Http
{
    public class HttpResponse : IHttpMessage
    {
        public string Version { get; set; }
        public int StatusCode { get; set; }
        public string ReasonPhrease { get; set; }
        public IList<HttpHeader> Headers { get; set; }
        public bool HasContent { get; set; }
        public Stream Content { get; set; }
    }
}