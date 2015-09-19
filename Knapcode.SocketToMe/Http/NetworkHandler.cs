using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Support;

namespace Knapcode.SocketToMe.Http
{
    public class NetworkHandler : HttpMessageHandler
    {
        private readonly Socket _socket;
        private const int BufferSize = 4096;

        public NetworkHandler()
        {
            _socket = null;
        }

        public NetworkHandler(Socket socket)
        {
            _socket = socket;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var stream = await GetStreamAsync(request);

            await WriteRequestAsync(request, stream);

            return await ReadResponseAsync(request, stream);
        }

        private static void ValidateRequest(HttpRequestMessage request)
        {
            if (request.RequestUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && request.RequestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Only HTTP and HTTPS are supported.");
            }

            if (request.Version != new Version(1, 1))
            {
                throw new NotSupportedException("Only HTTP/1.1 is supported.");
            }
        }

        private async Task<Stream> GetStreamAsync(HttpRequestMessage request)
        {
            // get the basic TCP stream
            var tcpClient = new TcpClient();
            if (_socket != null)
            {
                tcpClient.Client = _socket;
            }
            else
            {
                await tcpClient.ConnectAsync(request.RequestUri.DnsSafeHost, request.RequestUri.Port);
            }

            var httpStream = tcpClient.GetStream();

            // wrap in an SSL stream, if necessary
            if (request.RequestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                var httpsStream = new SslStream(httpStream);
                await httpsStream.AuthenticateAsClientAsync(request.RequestUri.DnsSafeHost);
                return httpsStream;
            }

            return httpStream;
        }

        private async Task WriteRequestAsync(HttpRequestMessage request, Stream stream)
        {
            byte[] bytes = null;
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false, true), BufferSize, true))
            {
                await writer.WriteLineAsync(string.Format("{0} {1} HTTP/{2}", request.Method.Method, request.RequestUri.PathAndQuery, request.Version));

                await writer.WriteLineAsync(string.Format("Host: {0}", request.RequestUri.Host));
                foreach (var header in request.Headers)
                {
                    await writer.WriteLineAsync(GetHeader(header));
                }

                if (request.Content != null)
                {
                    bytes = await request.Content.ReadAsByteArrayAsync();
                    request.Content.Headers.ContentLength = bytes.Length;

                    foreach (var header in request.Content.Headers)
                    {
                        await writer.WriteLineAsync(GetHeader(header));
                    }
                }

                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }

            if (bytes != null)
            {
                await new MemoryStream(bytes).CopyToAsync(stream);
            }
        }

        private string GetHeader(KeyValuePair<string, IEnumerable<string>> header)
        {
            return string.Format("{0}: {1}", header.Key, string.Join(",", header.Value));
        }

        private async Task<HttpResponseMessage> ReadResponseAsync(HttpRequestMessage request, Stream stream)
        {
            ByteStreamReader reader = new ByteStreamReader(stream, BufferSize, false);
            var response = await ReadResponseHeadAsync(request, reader);
            ReadResponseBody(response, reader);
            return response;
        }

        private static async Task<HttpResponseMessage> ReadResponseHeadAsync(HttpRequestMessage request, ByteStreamReader reader)
        {
            // initialize the response
            var response = new HttpResponseMessage { RequestMessage = request };

            // read the first line of the response
            string line = await reader.ReadLineAsync();
            string[] pieces = line.Split(new[] { ' ' }, 3);
            if (pieces[0] != "HTTP/1.1")
            {
                throw new HttpRequestException("The HTTP version the response is not supported.");
            }

            response.StatusCode = (HttpStatusCode)int.Parse(pieces[1]);
            response.ReasonPhrase = pieces[2];

            // read the headers
            response.Content = new ByteArrayContent(new byte[0]);
            while ((line = await reader.ReadLineAsync()) != null && line != string.Empty)
            {
                pieces = line.Split(new[] { ":" }, 2, StringSplitOptions.None);
                if (pieces[1].StartsWith(" "))
                {
                    pieces[1] = pieces[1].Substring(1);
                }

                var headers = HttpHeaderCategories.IsContentHeader(pieces[0]) ? (HttpHeaders)response.Content.Headers : response.Headers;
                headers.TryAddWithoutValidation(pieces[0], pieces[1]);
            }
            return response;
        }

        private static void ReadResponseBody(HttpResponseMessage response, ByteStreamReader reader)
        {
            if (response.RequestMessage.Method == HttpMethod.Head)
            {
                reader.Dispose();
                return;
            }

            HttpContent content = null;
            if (response.Headers.TransferEncodingChunked.GetValueOrDefault(false))
            {
                // read the body with chunked transfer encoding
                var remainingStream = reader.GetRemainingStream();
                var chunkedStream = new ChunkedStream(remainingStream);
                content = new StreamContent(chunkedStream);
            }
            else if (response.Content.Headers.ContentLength.HasValue)
            {
                // read the body with a content-length
                var remainingStream = reader.GetRemainingStream();
                var limitedStream = new LimitedStream(remainingStream, response.Content.Headers.ContentLength.Value);
                content = new StreamContent(limitedStream);
            }

            if (content != null)
            {
                // copy over the content headers
                foreach (var header in response.Content.Headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                response.Content = content;
            }
        }
    }
}