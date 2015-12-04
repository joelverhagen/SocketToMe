using System.Collections.Generic;
using System.IO;

namespace Knapcode.SocketToMe.Http
{
    public class HttpRequest : IHttpMessage
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
        public IList<HttpHeader> Headers { get; set; }
        public bool HasContent { get; set; }
        public Stream Content { get; set; }
    }
}