using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Knapcode.SocketToMe.Http
{
    public class XmlHttpMessageSerializer : IHttpMessageSerializer
    {
        private readonly IHttpMessageMapper _mapper;

        public XmlHttpMessageSerializer(IHttpMessageMapper mapper)
        {
            _mapper = mapper;
        }

        public async Task<IEnumerable<StoreEntry>> SerializeRequestAsync(Guid exchangeId, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var model = await _mapper.ToHttpRequestAsync(request, cancellationToken);
            return Serialize(exchangeId, "request", model);
        }

        public async Task<IEnumerable<StoreEntry>> SerializeResponseAsync(Guid exchangeId, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var model = await _mapper.ToHttpResponseAsync(response, cancellationToken);
            return Serialize(exchangeId, "response", model);
        }

        public Task<IEnumerable<StoreEntry>> SerializeExceptionAsync(Guid exchangeId, Exception exception, CancellationToken cancellationToken)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(exception.ToString()));
            var entries = new List<StoreEntry> {new StoreEntry {Key = GetKey(exchangeId, "exception"), Stream = stream}};
            return Task.FromResult((IEnumerable<StoreEntry>)entries);
        }

        private IEnumerable<StoreEntry> Serialize<T>(Guid exchangeId, string name, T model) where T : IHttpMessage
        {
            var entries = new List<StoreEntry>();
            if (model.Content != null)
            {
                entries.Add(new StoreEntry { Key = GetKey(exchangeId, name, "content"), Stream = model.Content });
                model.Content = null;
            }

            var serializer = new XmlSerializer(typeof(T));
            var stream = new MemoryStream();
            serializer.Serialize(stream, model);
            stream.Seek(0, SeekOrigin.Begin);
            entries.Add(new StoreEntry { Key = GetKey(exchangeId, name), Stream = stream });

            return entries;
        }

        private string GetKey(Guid exchangeId, params string[] pieces)
        {
            return string.Join("-", new[] { exchangeId.ToString("N") }.Concat(pieces));
        }
    }
}