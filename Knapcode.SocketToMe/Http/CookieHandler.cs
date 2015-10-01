using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public class CookieHandler : DelegatingHandler
    {
        private const string CookieKey = "Cookie";
        private const string SetCookieKey = "Set-Cookie";

        public CookieContainer CookieContainer { get; set; }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // get headers from the request
            string manualCookieValues = null;
            if (request.Headers.Contains(CookieKey))
            {
                var cookieValues = request.Headers.GetValues(CookieKey);
                request.Headers.Remove(CookieKey);
                manualCookieValues = string.Join("; ", cookieValues.Select(Trim));
            }

            // get headers from the cookie container
            if (CookieContainer != null)
            {
                var cookieValues = CookieContainer.GetCookieHeader(request.RequestUri);
                if (manualCookieValues != null)
                {
                    cookieValues = Trim(manualCookieValues + "; " + cookieValues);
                }

                if (!string.IsNullOrWhiteSpace(cookieValues))
                {
                    request.Headers.Add(CookieKey, cookieValues);
                }
            }

            var response = await base.SendAsync(request, cancellationToken);

            // part the response cookies
            if (CookieContainer != null && response.Headers.Contains(SetCookieKey))
            {
                var cookieHeaders = response.Headers.GetValues(SetCookieKey);
                foreach (var setCookieValue in cookieHeaders)
                {
                    CookieContainer.SetCookies(response.RequestMessage.RequestUri, setCookieValue);
                }
            }

            return response;
        }

        private string Trim(string cookieValues)
        {
            return cookieValues.Trim().Trim(';').Trim();
        }
    }
}