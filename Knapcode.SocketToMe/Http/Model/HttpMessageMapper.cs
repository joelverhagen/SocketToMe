using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public interface IHttpMessage
    {
        string Version { get; set; }
        IEnumerable<KeyValuePair<string, string>> Headers { get; set; }
        Stream Content { get; set; }
    }

    public interface IHttpMessageMapper
    {
        HttpRequestMessage ToHttpRequestMessage(HttpRequest request);
        HttpResponseMessage ToHttpResponseMessage(HttpResponse response);
        Task<HttpRequest> ToHttpRequestAsync(HttpRequestMessage request);
        Task<HttpResponse> ToHttpResponseAsync(HttpResponseMessage response);
    }

    public class HttpMessageMapper : IHttpMessageMapper
    {
        public HttpRequestMessage ToHttpRequestMessage(HttpRequest request)
        {
            var output = new HttpRequestMessage
            {
                Method = new HttpMethod(request.Method),
                RequestUri = new Uri(request.Url),
                Version = new Version(request.Version)
            };

            output.Content = MapContentAndHeaders(request, output.Headers, "request");

            return output;
        }

        public HttpResponseMessage ToHttpResponseMessage(HttpResponse response)
        {
            var output = new HttpResponseMessage
            {
                Version = new Version(response.Version),
                StatusCode = (HttpStatusCode)response.StatusCode,
                ReasonPhrase = response.ReasonPhrease
            };

            output.Content = MapContentAndHeaders(response, output.Headers, "response");

            return output;
        }

        public async Task<HttpRequest> ToHttpRequestAsync(HttpRequestMessage request)
        {
            var output = new HttpRequest
            {
                Method = request.Method.Method,
                Url = request.RequestUri.ToString(),
                Version = request.Version.ToString()
            };

            await MapContentAndHeadersAsync(request.Headers, request.Content, output).ConfigureAwait(false);

            return output;
        }

        public async Task<HttpResponse> ToHttpResponseAsync(HttpResponseMessage response)
        {
            var output = new HttpResponse
            {
                Version = response.Version.ToString(),
                StatusCode = (int)response.StatusCode,
                ReasonPhrease = response.ReasonPhrase
            };

            await MapContentAndHeadersAsync(response.Headers, response.Content, output).ConfigureAwait(false);

            return output;
        }

        private HttpContent MapContentAndHeaders(IHttpMessage message, HttpHeaders httpHeaders, string responseOrRequest)
        {
            var contentHeaders = message.Headers.Where(pair => !httpHeaders.TryAddWithoutValidation(pair.Key, pair.Value)).ToArray();
            HttpContent content = null;
            if (message.Content != null)
            {
                content = new StreamContent(message.Content);
                foreach (var header in contentHeaders)
                {
                    if (!content.Headers.TryAddWithoutValidation(header.Key, header.Value))
                    {
                        throw new InvalidOperationException($"The header '{header.Key}' could not be added to the {responseOrRequest} message or to the {responseOrRequest} content.");
                    }
                }
            }

            return content;
        }

        private async Task MapContentAndHeadersAsync(HttpHeaders httpHeaders, HttpContent content, IHttpMessage httpMessage)
        {
            var headersList = new List<KeyValuePair<string, string>>();
            foreach (var header in httpHeaders)
            {
                headersList.AddRange(header.Value.Select(value => new KeyValuePair<string, string>(header.Key, value)));
            }

            if (content != null)
            {
                foreach (var header in content.Headers)
                {
                    headersList.AddRange(header.Value.Select(value => new KeyValuePair<string, string>(header.Key, value)));
                }

                httpMessage.Content = await content.ReadAsStreamAsync().ConfigureAwait(false);
            }

            httpMessage.Headers = headersList;
        }
    }

}
