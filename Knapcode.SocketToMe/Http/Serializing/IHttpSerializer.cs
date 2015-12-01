using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public interface IHttpMessageSerializer
    {
        Task<IEnumerable<StoreEntry>> SerializeRequestAsync(Guid exchangeId, HttpRequestMessage request, CancellationToken cancellationToken);
        Task<IEnumerable<StoreEntry>> SerializeResponseAsync(Guid exchangeId, HttpResponseMessage response, CancellationToken cancellationToken);
        Task<IEnumerable<StoreEntry>> SerializeExceptionAsync(Guid exchangeId, Exception exception, CancellationToken cancellationToken);
    }
}
