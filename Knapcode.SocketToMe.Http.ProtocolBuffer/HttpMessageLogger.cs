using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf.Meta;

namespace Knapcode.SocketToMe.Http.ProtocolBuffer
{
    public class HttpMessageLogger : IHttpMessageLogger
    {
        public static readonly TypeModel TypeModel;
        
        static HttpMessageLogger()
        {
            var runtimeTypeModel = TypeModel.Create();

            var headerType = runtimeTypeModel.Add(typeof(HttpHeader), false);
            headerType.AddField(1, nameof(HttpHeader.Name)).IsRequired = true;
            headerType.AddField(2, nameof(HttpHeader.Value)).IsRequired = true;

            var requestType = runtimeTypeModel.Add(typeof(HttpRequest), false);
            requestType.AddField(1, nameof(HttpRequest.Method)).IsRequired = true;
            requestType.AddField(2, nameof(HttpRequest.Url)).IsRequired = true;
            requestType.AddField(3, nameof(HttpRequest.Version)).IsRequired = true;
            requestType.AddField(4, nameof(HttpRequest.Headers)).IsRequired = true;
            requestType.AddField(5, nameof(HttpRequest.HasContent)).IsRequired = true;

            var responseType = runtimeTypeModel.Add(typeof(HttpResponse), false);
            responseType.AddField(1, nameof(HttpResponse.Version)).IsRequired = true;
            responseType.AddField(2, nameof(HttpResponse.StatusCode)).IsRequired = true;
            responseType.AddField(3, nameof(HttpResponse.ReasonPhrease)).IsRequired = true;
            responseType.AddField(4, nameof(HttpResponse.Headers)).IsRequired = true;
            responseType.AddField(5, nameof(HttpResponse.HasContent)).IsRequired = true;

            TypeModel = runtimeTypeModel.Compile();
        }

        private readonly IStore _store;
        private readonly IHttpMessageMapper _mapper;

        public HttpMessageLogger(IStore store, IHttpMessageMapper mapper)
        {
            _store = store;
            _mapper = mapper;
        }

        public async Task LogRequestAsync(Guid exchangeId, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // write the model
            var model = await _mapper.ToHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
            var modelKey = GetKey(exchangeId, "request");
            await SetModelAsync(modelKey, model, cancellationToken).ConfigureAwait(false);
            
            if (model.HasContent)
            {
                // write the content
                var contentKey = GetKey(exchangeId, "request", "content");
                request.Content = await SetAndGetContentAsync(contentKey, model.Content, request.Content.Headers, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task LogResponseAsync(Guid exchangeId, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            // write the model
            var model = await _mapper.ToHttpResponseAsync(response, cancellationToken).ConfigureAwait(false);
            var modelKey = GetKey(exchangeId, "response");
            await SetModelAsync(modelKey, model, cancellationToken).ConfigureAwait(false);

            if (model.HasContent)
            {
                // write the content
                var contentKey = GetKey(exchangeId, "response", "content");
                response.Content = await SetAndGetContentAsync(contentKey, model.Content, response.Content.Headers, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task LogExceptionAsync(Guid exchangeId, Exception exception, CancellationToken cancellationToken)
        {
            var exceptionString = exception.ToString();
            var modelStream = new MemoryStream(new UTF8Encoding(false).GetBytes(exceptionString));
            var exceptionKey = GetKey(exchangeId, "exception");
            await _store.SetAsync(exceptionKey, modelStream, cancellationToken).ConfigureAwait(false);
        }

        private string GetKey(Guid exchangeId, params string[] inputPieces)
        {
            var allPieces = new[] {exchangeId.ToString("N")}.Concat(inputPieces);
            return string.Join("-", allPieces);
        }

        private async Task SetModelAsync<T>(string key, T model, CancellationToken cancellationToken) where T : IHttpMessage
        {
            var modelStream = new MemoryStream();
            TypeModel.Serialize(modelStream, model);
            modelStream.Seek(0, SeekOrigin.Begin);
            await _store.SetAsync(key, modelStream, cancellationToken).ConfigureAwait(false);
        }

        private async Task<HttpContent> SetAndGetContentAsync(string key, Stream content, HttpHeaders contentHeaders, CancellationToken cancellationToken)
        {
            // write the content
            await _store.SetAsync(key, content, cancellationToken).ConfigureAwait(false);

            // read the content back
            var contentStream = await _store.GetAsync(key, cancellationToken).ConfigureAwait(false);
            var streamContent = new StreamContent(contentStream);
            foreach (var header in contentHeaders)
            {
                if (!streamContent.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    throw new InvalidOperationException($"The content header '{header.Key}' could not be added back to the logged content.");
                }
            }

            return streamContent;
        }
    }
}