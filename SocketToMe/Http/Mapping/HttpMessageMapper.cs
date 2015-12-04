using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public interface IHttpMessageMapper
    {
        HttpRequestMessage ToHttpMessage(HttpRequest request);
        HttpResponseMessage ToHttpMessage(HttpResponse response);
        Task<HttpRequest> ToHttpAsync(HttpRequestMessage request, CancellationToken cancellationToken);
        Task<HttpResponse> ToHttpAsync(HttpResponseMessage response, CancellationToken cancellationToken);
    }

    public class HttpMessageMapper : IHttpMessageMapper
    {
        public HttpRequestMessage ToHttpMessage(HttpRequest request)
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

        public HttpResponseMessage ToHttpMessage(HttpResponse response)
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

        public async Task<HttpRequest> ToHttpAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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

        public async Task<HttpResponse> ToHttpAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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
            var contentHeaders = message
                .Headers?
                .Where(pair => !httpHeaders.TryAddWithoutValidation(pair.Name, pair.Value))
                .ToArray() ?? Enumerable.Empty<HttpHeader>();

            HttpContent content = null;
            if (message.Content != null)
            {
                content = new StreamContent(message.Content);
                foreach (var header in contentHeaders)
                {
                    if (!content.Headers.TryAddWithoutValidation(header.Name, header.Value))
                    {
                        throw new InvalidOperationException($"The header '{header.Name}' could not be added to the {responseOrRequest} message or to the {responseOrRequest} content.");
                    }
                }
            }

            return content;
        }

        private async Task MapContentAndHeadersAsync(HttpHeaders httpHeaders, HttpContent content, IHttpMessage httpMessage)
        {
            var headersList = new List<HttpHeader>();
            foreach (var header in httpHeaders)
            {
                headersList.AddRange(header.Value.Select(value => new HttpHeader { Name = header.Key, Value = value }));
            }

            if (content != null)
            {
                foreach (var header in content.Headers)
                {
                    headersList.AddRange(header.Value.Select(value => new HttpHeader {Name = header.Key, Value = value}));
                }

                httpMessage.HasContent = true;
                httpMessage.Content = await content.ReadAsStreamAsync().ConfigureAwait(false);
            }
            else
            {
                httpMessage.HasContent = false;
            }

            httpMessage.Headers = headersList;
        }
    }

}
