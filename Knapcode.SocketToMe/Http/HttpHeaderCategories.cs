using System;
using System.Collections.Generic;
using System.Linq;

namespace Knapcode.SocketToMe.Http
{
    public static class HttpHeaderCategories
    {
        private static readonly ISet<string> RequestHeaderSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly ISet<string> ResponseHeaderSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly ISet<string> ContentHeaderSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly IEnumerable<Header> Headers = new[]
        {
            new Header {Name = "Accept", Category = HeaderCategory.Request},
            new Header {Name = "Accept-Charset", Category = HeaderCategory.Request},
            new Header {Name = "Accept-Encoding", Category = HeaderCategory.Request},
            new Header {Name = "Accept-Language", Category = HeaderCategory.Request},
            new Header {Name = "Accept-Ranges", Category = HeaderCategory.Response},
            new Header {Name = "Age", Category = HeaderCategory.Response},
            new Header {Name = "Allow", Category = HeaderCategory.Content},
            new Header {Name = "Authorization", Category = HeaderCategory.Request},
            new Header {Name = "Cache-Control", Category = HeaderCategory.General},
            new Header {Name = "Connection", Category = HeaderCategory.General},
            new Header {Name = "Content-Disposition", Category = HeaderCategory.Content},
            new Header {Name = "Content-Encoding", Category = HeaderCategory.Content},
            new Header {Name = "Content-Language", Category = HeaderCategory.Content},
            new Header {Name = "Content-Length", Category = HeaderCategory.Content},
            new Header {Name = "Content-Location", Category = HeaderCategory.Content},
            new Header {Name = "Content-MD5", Category = HeaderCategory.Content},
            new Header {Name = "Content-Range", Category = HeaderCategory.Content},
            new Header {Name = "Content-Type", Category = HeaderCategory.Content},
            new Header {Name = "Date", Category = HeaderCategory.General},
            new Header {Name = "ETag", Category = HeaderCategory.Response},
            new Header {Name = "Expect", Category = HeaderCategory.Request},
            new Header {Name = "Expires", Category = HeaderCategory.Content},
            new Header {Name = "From", Category = HeaderCategory.Request},
            new Header {Name = "Host", Category = HeaderCategory.Request},
            new Header {Name = "If-Match", Category = HeaderCategory.Request},
            new Header {Name = "If-Modified-Since", Category = HeaderCategory.Request},
            new Header {Name = "If-None-Match", Category = HeaderCategory.Request},
            new Header {Name = "If-Range", Category = HeaderCategory.Request},
            new Header {Name = "If-Unmodified-Since", Category = HeaderCategory.Request},
            new Header {Name = "Last-Modified", Category = HeaderCategory.Content},
            new Header {Name = "Location", Category = HeaderCategory.Response},
            new Header {Name = "Max-Forwards", Category = HeaderCategory.Request},
            new Header {Name = "Pragma", Category = HeaderCategory.General},
            new Header {Name = "Proxy-Authenticate", Category = HeaderCategory.Response},
            new Header {Name = "Proxy-Authorization", Category = HeaderCategory.Request},
            new Header {Name = "Range", Category = HeaderCategory.Request},
            new Header {Name = "Referer", Category = HeaderCategory.Request},
            new Header {Name = "Retry-After", Category = HeaderCategory.Response},
            new Header {Name = "Server", Category = HeaderCategory.Response},
            new Header {Name = "TE", Category = HeaderCategory.Request},
            new Header {Name = "Trailer", Category = HeaderCategory.General},
            new Header {Name = "Transfer-Encoding", Category = HeaderCategory.General},
            new Header {Name = "Upgrade", Category = HeaderCategory.General},
            new Header {Name = "User-Agent", Category = HeaderCategory.Request},
            new Header {Name = "Vary", Category = HeaderCategory.Response},
            new Header {Name = "Via", Category = HeaderCategory.General},
            new Header {Name = "Warning", Category = HeaderCategory.General},
            new Header {Name = "WWW-Authenticate", Category = HeaderCategory.Response}
        };

        static HttpHeaderCategories()
        {
            foreach (var header in Headers)
            {
                switch (header.Category)
                {
                    case HeaderCategory.General:
                        RequestHeaderSet.Add(header.Name);
                        ResponseHeaderSet.Add(header.Name);
                        break;

                    case HeaderCategory.Request:
                        RequestHeaderSet.Add(header.Name);
                        break;

                    case HeaderCategory.Response:
                        ResponseHeaderSet.Add(header.Name);
                        break;

                    case HeaderCategory.Content:
                        ContentHeaderSet.Add(header.Name);
                        break;
                }
            }
        }

        public static IEnumerable<string> RequestHeaders
        {
            get { return RequestHeaderSet.ToArray(); }
        }

        public static IEnumerable<string> ResponseHeaders
        {
            get { return ResponseHeaderSet.ToArray(); }
        }

        public static IEnumerable<string> ContentHeaders
        {
            get { return ContentHeaderSet.ToArray(); }
        }

        public static bool IsRequestHeader(string header)
        {
            return RequestHeaderSet.Contains(header);
        }

        public static bool IsResponseHeader(string header)
        {
            return ResponseHeaderSet.Contains(header);
        }

        public static bool IsContentHeader(string header)
        {
            return ContentHeaderSet.Contains(header);
        }

        [Flags]
        private enum HeaderCategory
        {
            General,
            Request,
            Response,
            Content
        }

        private class Header
        {
            public string Name { get; set; }
            public HeaderCategory Category { get; set; }
        }
    }
}