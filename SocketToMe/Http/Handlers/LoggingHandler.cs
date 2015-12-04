using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public class LoggingHandler : DelegatingHandler
    {
        public const string ExchangeIdPropertyKey = "Knapcode.SocketToMe.Http.LoggingHandler.ExchangeId";
        private readonly IHttpMessageLogger _logger;

        public LoggingHandler(IHttpMessageLogger logger)
        {
            _logger = logger;
            StoreExchangeId = true;
        }

        public bool StoreExchangeId { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Guid exchangeId = Guid.NewGuid();

            if (StoreExchangeId)
            {
                request.Properties[ExchangeIdPropertyKey] = exchangeId;
            }

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
