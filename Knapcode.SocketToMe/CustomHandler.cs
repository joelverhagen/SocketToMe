using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe
{
    public class CustomHandler : HttpMessageHandler
    {
        public const int DefaultBufferSize = 4096;

        public CustomHandler()
        {
            BufferSize = DefaultBufferSize;
        }

        public int BufferSize { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int bufferSize = BufferSize;

            if (request.RequestUri.Scheme == "https")
            {
                throw new NotSupportedException("HTTPS is not supported.");
            }

            // var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IPv4);
            var tcpClient = new TcpClient(request.RequestUri.DnsSafeHost, request.RequestUri.Port);
            using (NetworkStream networkStream = tcpClient.GetStream())
            {
                // send the request
                await WriteRequestAsync(networkStream, bufferSize, request);

                // read the request
                return await ReadResponseAsync(networkStream, bufferSize);
            }
        }

        private async Task WriteRequestAsync(Stream stream, int bufferSize, HttpRequestMessage request)
        {
            byte[] bytes = null;
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false, true), bufferSize, true))
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
                await request.Content.CopyToAsync(stream);
            }
        }

        private string GetHeader(KeyValuePair<string, IEnumerable<string>> header)
        {
            return string.Format("{0}: {1}", header.Key, string.Join(",", header.Value));
        }

        private async Task<HttpResponseMessage> ReadResponseAsync(Stream stream, int bufferSize)
        {
            // initialize the response
            var contentStream = new MemoryStream();
            var response = new HttpResponseMessage {Content = new StreamContent(contentStream)};

            // read the first line of the response
            var reader = new ByteStreamReader(stream, bufferSize, false);
            string line = await reader.ReadLineAsync();
            string[] pieces = line.Split(new[] {' '}, 3);
            if (pieces[0] != "HTTP/1.1")
            {
                throw new HttpRequestException("The HTTP version the response is not supported.");
            }

            response.StatusCode = (HttpStatusCode) int.Parse(pieces[1]);
            response.ReasonPhrase = pieces[2];

            // read the headers
            while ((line = await reader.ReadLineAsync()) != null && line != string.Empty)
            {
                pieces = line.Split(new[] {": "}, 2, StringSplitOptions.None);
                if (!response.Headers.TryAddWithoutValidation(pieces[0], pieces[1]))
                {
                    response.Content.Headers.Add(pieces[0], pieces[1]);
                }
            }

            if (response.Content.Headers.ContentLength.HasValue)
            {
                long contentLength = response.Content.Headers.ContentLength.Value;
                var buffer = new byte[contentLength];
                int read = await reader.ReadAsync(buffer, 0, (int) contentLength);
                contentStream.Write(buffer, 0, read);
                contentStream.Position = 0;
            }

            return response;
        }
    }
}