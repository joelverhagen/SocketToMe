using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public class LoggingHandler : DelegatingHandler
    {
        private readonly IHttpMessageLogger _logger;

        public LoggingHandler(IHttpMessageLogger logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Guid exchangeId = Guid.NewGuid();
            await _logger.LogAsync(exchangeId, request, cancellationToken).ConfigureAwait(false);

            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch(Exception e)
            {
                await _logger.LogAsync(exchangeId, e, cancellationToken).ConfigureAwait(false);
                throw;
            }
            
            await _logger.LogAsync(exchangeId, response, cancellationToken).ConfigureAwait(false);
            return response;
        }
    }
}
