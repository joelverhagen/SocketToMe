using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Knapcode.SocketToMe.Http.ProtocolBuffer
{
    public interface IHttpMessageStore
    {
        Task SetAsync(Guid exchangeId, HttpRequestMessage request, CancellationToken cancellationToken);
        Task SetAsync(Guid exchangeId, HttpResponseMessage response, CancellationToken cancellationToken);
        Task SetAsync(Guid exchangeId, Exception exception, CancellationToken cancellationToken);
        Task<HttpRequestMessage> GetRequestAsync(Guid exchangeId, CancellationToken cancellationToken);
        Task<HttpResponseMessageOrException> GetResponseOrExceptionAsync(Guid exchangeId, CancellationToken cancellationToken);
    }

    public class HttpMessageStore : IHttpMessageStore
    {
        public static readonly TypeModel TypeModel;

        static HttpMessageStore()
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

            var responseOrExceptionType = runtimeTypeModel.Add(typeof(HttpResponseOrException), false);
            responseOrExceptionType.AddField(1, nameof(HttpResponseOrException.Response)).IsRequired = false;
            responseOrExceptionType.AddField(2, nameof(HttpResponseOrException.ExceptionString)).IsRequired = false;

            TypeModel = runtimeTypeModel.Compile();
        }

        private readonly IStore _store;
        private readonly IHttpMessageMapper _mapper;

        public HttpMessageStore(IStore store, IHttpMessageMapper mapper)
        {
            _store = store;
            _mapper = mapper;
        }

        public async Task SetAsync(Guid exchangeId, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // map to a simpler model
            var storedModel = await _mapper.ToHttpAsync(request, cancellationToken).ConfigureAwait(false);

            // write the content
            if (storedModel.HasContent)
            {
                
                var contentKey = GetRequestContentKey(exchangeId);
                request.Content = await SetAndGetContentAsync(contentKey, storedModel.Content, request.Content.Headers, cancellationToken).ConfigureAwait(false);
            }

            // write the model
            var modelKey = GetRequestKey(exchangeId);
            await SetModelAsync(modelKey, storedModel, cancellationToken).ConfigureAwait(false);
        }

        public async Task SetAsync(Guid exchangeId, HttpResponseMessage response, CancellationToken cancellationToken)
        {
            // map to a simpler model
            var responseModel = await _mapper.ToHttpAsync(response, cancellationToken).ConfigureAwait(false);

            // write the content
            if (responseModel.HasContent)
            {
                var contentKey = GetResponseContentKey(exchangeId);
                response.Content = await SetAndGetContentAsync(contentKey, responseModel.Content, response.Content.Headers, cancellationToken).ConfigureAwait(false);
            }

            // write the model
            var storedModel = new HttpResponseOrException {Response = responseModel, ExceptionString = null};
            var modelKey = GetResponseOrExceptionKey(exchangeId);
            await SetModelAsync(modelKey, storedModel, cancellationToken).ConfigureAwait(false);
        }

        public async Task SetAsync(Guid exchangeId, Exception exception, CancellationToken cancellationToken)
        {
            // write the model
            var exceptionString = exception.ToString();
            var storedModel = new HttpResponseOrException {Response = null, ExceptionString = exceptionString};
            var modelKey = GetResponseOrExceptionKey(exchangeId);
            await SetModelAsync(modelKey, storedModel, cancellationToken).ConfigureAwait(false);
        }

        public async Task<HttpRequestMessage> GetRequestAsync(Guid exchangeId, CancellationToken cancellationToken)
        {
            // get the model
            var modelKey = GetRequestKey(exchangeId);
            var storedModel = await GetModelAsync<HttpRequest>(modelKey, cancellationToken).ConfigureAwait(false);
            if (storedModel == null)
            {
                return null;
            }

            // get the content
            if (storedModel.HasContent)
            {
                var contentKey = GetRequestContentKey(exchangeId);
                var content = await _store.GetAsync(contentKey, cancellationToken).ConfigureAwait(false);
                storedModel.Content = content;
            }

            return _mapper.ToHttpMessage(storedModel);
        }

        public async Task<HttpResponseMessageOrException> GetResponseOrExceptionAsync(Guid exchangeId, CancellationToken cancellationToken)
        {
            // get the model
            var modelKey = GetResponseOrExceptionKey(exchangeId);
            var storedModel = await GetModelAsync<HttpResponseOrException>(modelKey, cancellationToken).ConfigureAwait(false);
            if (storedModel == null)
            {
                return null;
            }
            
            if (storedModel.Response == null)
            {
                return new HttpResponseMessageOrException {ExceptionString = storedModel.ExceptionString};
            }

            // get the content
            if (storedModel.Response.HasContent)
            {
                var contentKey = GetResponseContentKey(exchangeId);
                var content = await _store.GetAsync(contentKey, cancellationToken).ConfigureAwait(false);
                storedModel.Response.Content = content;
            }

            var response = _mapper.ToHttpMessage(storedModel.Response);
            return new HttpResponseMessageOrException {Response = response, ExceptionString = null};
        }

        public Task<string> GetExceptionStringAsync(Guid exchangeId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private string GetKey(Guid exchangeId, params string[] inputPieces)
        {
            var allPieces = new[] { exchangeId.ToString("N") }.Concat(inputPieces);
            return string.Join("-", allPieces);
        }

        private async Task SetModelAsync<T>(string key, T model, CancellationToken cancellationToken)
        {
            var modelStream = new MemoryStream();
            TypeModel.SerializeWithLengthPrefix(modelStream, model, model.GetType(), PrefixStyle.Fixed32BigEndian, -1);
            modelStream.Seek(0, SeekOrigin.Begin);
            await _store.SetAsync(key, modelStream, cancellationToken).ConfigureAwait(false);
        }

        private async Task<T> GetModelAsync<T>(string key, CancellationToken cancellationToken) 
        {
            var modelStream = await _store.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (modelStream == null)
            {
                return default(T);
            }

            return (T)TypeModel.DeserializeWithLengthPrefix(modelStream, null, typeof(T), PrefixStyle.Fixed32BigEndian, -1);
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

        private string GetRequestKey(Guid exchangeId)
        {
            return GetKey(exchangeId, "request");
        }

        private string GetRequestContentKey(Guid exchangeId)
        {
            return GetKey(exchangeId, "request", "content");
        }

        private string GetResponseOrExceptionKey(Guid exchangeId)
        {
            return GetKey(exchangeId, "response");
        }

        private string GetResponseContentKey(Guid exchangeId)
        {
            return GetKey(exchangeId, "response", "content");
        }
    }
}