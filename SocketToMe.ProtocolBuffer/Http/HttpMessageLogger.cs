using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Http;

namespace Knapcode.SocketToMe.ProtocolBuffer.Http
{
    public class HttpMessageLogger : IHttpMessageLogger
    {
        private readonly IHttpMessageStore _store;

        public HttpMessageLogger(IHttpMessageStore store)
        {
            _store = store;
        }

        public Task LogAsync(Guid exchangeId, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _store.SetAsync(exchangeId, request, cancellationToken);
        }

        public Task LogAsync(Guid exchangeId, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            return _store.SetAsync(exchangeId, response, cancellationToken);
        }

        public Task LogAsync(Guid exchangeId, Exception exception, CancellationToken cancellationToken)
        {
            return _store.SetAsync(exchangeId, exception, cancellationToken);
        }
    }
}