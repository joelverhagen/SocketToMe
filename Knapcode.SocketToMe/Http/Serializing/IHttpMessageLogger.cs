using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public interface IHttpMessageLogger
    {
        Task LogRequestAsync(Guid exchangeId, HttpRequestMessage request, CancellationToken cancellationToken);
        Task LogResponseAsync(Guid exchangeId, HttpResponseMessage response, CancellationToken cancellationToken);
        Task LogExceptionAsync(Guid exchangeId, Exception exception, CancellationToken cancellationToken);
    }
}