using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Reflection;

namespace Knapcode.SocketToMe.Http
{
    public static class HttpHeaderCategories
    {
        private static readonly ISet<string> HttpGeneralHeaders;
        private static readonly ISet<string> HttpRequestHeaders;
        private static readonly ISet<string> HttpResponseHeaders;
        private static readonly ISet<string> HttpContentHeaders;

        static HttpHeaderCategories()
        {
            HttpGeneralHeaders = GetKnownHeaders(typeof (HttpContentHeaders).Assembly.GetType("System.Net.Http.Headers.HttpGeneralHeaders"));
            HttpRequestHeaders = GetKnownHeaders(typeof (HttpRequestHeaders));
            HttpResponseHeaders = GetKnownHeaders(typeof (HttpResponseHeaders));
            HttpContentHeaders = GetKnownHeaders(typeof (HttpContentHeaders));
        }

        private static ISet<string> GetKnownHeaders(Type headerType)
        {
            var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var addKnownHeaders = headerType.GetMethod("AddKnownHeaders", BindingFlags.Static | BindingFlags.NonPublic);
            addKnownHeaders.Invoke(null, new object[] {headers});
            return headers;
        }

        public static bool IsGeneralHeader(string header)
        {
            return HttpGeneralHeaders.Contains(header);
        }

        public static bool IsRequestHeader(string header)
        {
            return HttpRequestHeaders.Contains(header);
        }

        public static bool IsResponseHeader(string header)
        {
            return HttpResponseHeaders.Contains(header);
        }

        public static bool IsContentHeader(string header)
        {
            return HttpContentHeaders.Contains(header);
        }
    }
}