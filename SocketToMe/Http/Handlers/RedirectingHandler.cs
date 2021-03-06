using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    /// <summary>A delegating handler that handles HTTP redirects (301, 302, 303, 307, and 308).</summary>
    public class RedirectingHandler : DelegatingHandler
    {
        private static readonly ISet<HttpStatusCode> RedirectStatusCodes = new HashSet<HttpStatusCode>(new[]
        {
            HttpStatusCode.MovedPermanently,
            HttpStatusCode.Found,
            HttpStatusCode.SeeOther,
            HttpStatusCode.TemporaryRedirect,
            (HttpStatusCode) 308
        });

        private static readonly ISet<HttpStatusCode> KeepRequestBodyRedirectStatusCodes = new HashSet<HttpStatusCode>(new[]
        {
            HttpStatusCode.TemporaryRedirect,
            (HttpStatusCode) 308
        });

        /// <summary>Initializes a new instance of the <see cref="RedirectingHandler" /> class.</summary>
        public RedirectingHandler()
        {
            AllowAutoRedirect = true;
            MaxAutomaticRedirections = 50;
            DisableInnerAutoRedirect = true;
            DownloadContentOnRedirect = false;
        }

        /// <summary>Gets or sets a value that indicates whether the handler should follow redirection responses.</summary>
        public bool AllowAutoRedirect { get; set; }

        /// <summary>Gets or sets the maximum number of redirects that the handler follows.</summary>
        public int MaxAutomaticRedirections { get; set; }

        /// <summary>Gets or sets a value indicating whether the response body should be downloaded before each redirection.</summary>
        public bool DownloadContentOnRedirect { get; set; }

        /// <summary>
        /// Gets or sets a value indicating inner redirections on <see cref="HttpClientHandler" /> and
        /// <see cref="RedirectingHandler" /> should be disabled.
        /// </summary>
        public bool DisableInnerAutoRedirect { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (DisableInnerAutoRedirect)
            {
                // find the inner-most handler
                var innerHandler = InnerHandler;
                while (innerHandler is DelegatingHandler)
                {
                    var redirectingHandler = innerHandler as RedirectingHandler;
                    if (redirectingHandler != null)
                    {
                        redirectingHandler.AllowAutoRedirect = false;
                    }

                    innerHandler = ((DelegatingHandler) innerHandler).InnerHandler;
                }

                var httpClientHandler = innerHandler as HttpClientHandler;
                if (httpClientHandler != null)
                {
                    httpClientHandler.AllowAutoRedirect = false;
                }
            }

            // buffer the request body, to allow re-use in redirects
            HttpContent requestBody = null;
            if (AllowAutoRedirect && request.Content != null)
            {
                var buffer = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                requestBody = new ByteArrayContent(buffer);
                foreach (var header in request.Content.Headers)
                {
                    requestBody.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // make a copy of the request headers
            var requestHeaders = request
                .Headers
                .Select(p => new KeyValuePair<string, string[]>(p.Key, p.Value.ToArray()))
                .ToArray();

            // send the initial request
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            var redirectCount = 0;
            string locationString;
            while (AllowAutoRedirect && redirectCount < MaxAutomaticRedirections && TryGetRedirectLocation(response, out locationString))
            {
                if (DownloadContentOnRedirect && response.Content != null)
                {
                    await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }

                if (response.RequestMessage == null)
                {
                    throw new InvalidOperationException("A response recieved from the inner handler did not have a request message.");
                }

                var previousRequestUri = response.RequestMessage.RequestUri;

                // Credit where credit is due: https://github.com/kennethreitz/requests/blob/master/requests/sessions.py
                // allow redirection without a scheme
                if (locationString.StartsWith("//"))
                {
                    locationString = previousRequestUri.Scheme + ":" + locationString;
                }

                // allow relative redirects
                Uri nextRequestUri;
                if (locationString.StartsWith("/") || !Uri.TryCreate(locationString, UriKind.Absolute, out nextRequestUri))
                {
                    nextRequestUri = new Uri(previousRequestUri, locationString);
                }

                // override previous method
                var nextMethod = response.RequestMessage.Method;
                if ((response.StatusCode == HttpStatusCode.Moved && nextMethod == HttpMethod.Post) ||
                    (response.StatusCode == HttpStatusCode.Found && nextMethod != HttpMethod.Head) ||
                    (response.StatusCode == HttpStatusCode.SeeOther && nextMethod != HttpMethod.Head))
                {
                    nextMethod = HttpMethod.Get;
                    requestBody = null;
                }

                if (!KeepRequestBodyRedirectStatusCodes.Contains(response.StatusCode))
                {
                    requestBody = null;
                }

                // build the next request
                var nextRequest = new HttpRequestMessage(nextMethod, nextRequestUri)
                {
                    Content = requestBody,
                    Version = request.Version
                };

                foreach (var header in requestHeaders)
                {
                    nextRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                foreach (var pair in request.Properties)
                {
                    nextRequest.Properties.Add(pair.Key, pair.Value);
                }

                // send the next request
                response = await base.SendAsync(nextRequest, cancellationToken).ConfigureAwait(false);

                request = nextRequest;
                redirectCount++;
            }

            return response;
        }

        private static bool TryGetRedirectLocation(HttpResponseMessage response, out string location)
        {
            IEnumerable<string> locations;

            if (RedirectStatusCodes.Contains(response.StatusCode) &&
                response.Headers.TryGetValues("Location", out locations) &&
                (locations = locations.ToArray()).Count() == 1 &&
                !string.IsNullOrWhiteSpace(locations.First()))
            {
                location = locations.First().Trim();
                return true;
            }

            location = null;
            return false;
        }
    }
}