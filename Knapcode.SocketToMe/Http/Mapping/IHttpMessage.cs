using System.Collections.Generic;
using System.IO;

namespace Knapcode.SocketToMe.Http
{
    public interface IHttpMessage
    {
        string Version { get; set; }
        IList<HttpHeader> Headers { get; set; }
        bool HasContent { get; set; }
        Stream Content { get; set; }
    }
}