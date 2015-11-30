using System.Collections.Generic;
using System.IO;

namespace Knapcode.SocketToMe.Http
{
    public class HttpResponse : IHttpMessage
    {
        public string Version { get; set; }
        public int StatusCode { get; set; }
        public string ReasonPhrease { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Headers { get; set; }
        public Stream Content { get; set; }
    }
}