using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Support;

namespace Knapcode.SocketToMe.Http
{
    public class HttpSocketClient
    {
        private const int BufferSize = 4096;
        private static readonly HttpMethod ConnectMethod = new HttpMethod("CONNECT");
        private static readonly ISet<HttpMethod> MethodsWithoutHostHeader = new HashSet<HttpMethod> { ConnectMethod };
        private static readonly ISet<HttpMethod> MethodsWithoutRequestBody = new HashSet<HttpMethod> { ConnectMethod, HttpMethod.Head };
        private static readonly ISet<HttpMethod> MethodsWithoutResponseBody = new HashSet<HttpMethod> { ConnectMethod, HttpMethod.Head };

        public async Task<Stream> GetStreamAsync(Socket socket, HttpRequestMessage request)
        {
            Stream networkStream = new NetworkStream(socket);

            if (request.RequestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                var httpsStream = new SslStream(networkStream);

                await httpsStream.AuthenticateAsClientAsync(request.RequestUri.DnsSafeHost);

                networkStream = httpsStream;
            }

            return networkStream;
        }

        public async Task SendRequestAsync(Stream stream, HttpRequestMessage request)
        {
            ValidateRequest(request);

            await WriteRequestAsync(stream, request);
        }

        public async Task<HttpResponseMessage> ReceiveResponseAsync(Stream stream, HttpRequestMessage request)
        {
            ByteStreamReader reader = new ByteStreamReader(stream, BufferSize, false);

            var response = await ReadResponseHeadAsync(reader, request);

            if (!MethodsWithoutResponseBody.Contains(request.Method))
            {
                ReadResponseBody(reader, response);
            }

            return response;
        }

        private void ValidateRequest(HttpRequestMessage request)
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

        private async Task WriteRequestAsync(Stream stream, HttpRequestMessage request)
        {
            byte[] bytes = null;
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false, true), BufferSize, true))
            {
                var location = request.Method != ConnectMethod ? request.RequestUri.PathAndQuery : $"{request.RequestUri.DnsSafeHost}:{request.RequestUri.Port}";
                await writer.WriteLineAsync($"{request.Method.Method} {location} HTTP/{request.Version}");
                
                if (!request.Headers.Contains("Host") && !MethodsWithoutHostHeader.Contains(request.Method))
                {
                    await writer.WriteLineAsync($"Host: {request.RequestUri.Host}");
                }

                foreach (var header in request.Headers)
                {
                    await writer.WriteLineAsync(GetHeader(header));
                }

                if (request.Content != null && !MethodsWithoutRequestBody.Contains(request.Method))
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

            await stream.FlushAsync();
        }

        private string GetHeader(KeyValuePair<string, IEnumerable<string>> header)
        {
            return $"{header.Key}: {string.Join(",", header.Value)}";
        }

        private async Task<HttpResponseMessage> ReadResponseHeadAsync(ByteStreamReader reader, HttpRequestMessage request)
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

        private void ReadResponseBody(ByteStreamReader reader, HttpResponseMessage response)
        {
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
