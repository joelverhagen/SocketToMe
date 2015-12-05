using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public interface IHttpMessageLogger
    {
        Task LogAsync(ExchangeId exchangeId, HttpRequestMessage request, CancellationToken cancellationToken);
        Task LogAsync(ExchangeId exchangeId, HttpResponseMessage response, CancellationToken cancellationToken);
        Task LogAsync(ExchangeId exchangeId, Exception exception, CancellationToken cancellationToken);
    }
}