using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public interface IHttpMessageLogger
    {
        Task LogAsync(Guid exchangeId, HttpRequestMessage request, CancellationToken cancellationToken);
        Task LogAsync(Guid exchangeId, HttpResponseMessage response, CancellationToken cancellationToken);
        Task LogAsync(Guid exchangeId, Exception exception, CancellationToken cancellationToken);
    }
}