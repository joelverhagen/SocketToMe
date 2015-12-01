using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Http
{
    public class HttpMessageStoreLogger : IHttpMessageLogger
    {
        private readonly IStore _store;
        private readonly IHttpMessageSerializer _serializer;

        public HttpMessageStoreLogger(IStore store, IHttpMessageSerializer serializer)
        {
            _store = store;
            _serializer = serializer;
        }

        public async Task LogRequestAsync(Guid exchangeId, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var entries = await _serializer.SerializeRequestAsync(exchangeId, request, cancellationToken);
            await SetEntries(entries, cancellationToken);
        }

        public async Task LogResponseAsync(Guid exchangeId, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var entries = await _serializer.SerializeResponseAsync(exchangeId, response, cancellationToken);
            await SetEntries(entries, cancellationToken);
        }

        public async Task LogExceptionAsync(Guid exchangeId, Exception exception, CancellationToken cancellationToken)
        {
            var entries = await _serializer.SerializeExceptionAsync(exchangeId, exception, cancellationToken);
            await SetEntries(entries, cancellationToken);
        }

        private async Task SetEntries(IEnumerable<StoreEntry> entries, CancellationToken cancellationToken)
        {
            foreach (var entry in entries)
            {
                await _store.SetAsync(entry.Key, entry.Stream, cancellationToken);
            }
        }
    }
}