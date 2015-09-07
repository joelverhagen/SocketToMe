using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Support;

namespace Knapcode.SocketToMe.Http
{
    public class DecompressingHandler : DelegatingHandler
    {
        /// <summary>Indicates zlib using the DEFLATE compression algorithm.</summary>
        private const byte ValidZlibCmfByte = 0x78;

        /// <summary>Indicates no dictionary and one of the four possible compression levels.</summary>
        private static readonly ISet<byte> ValidZlibFlgBytes = new HashSet<byte>(new[]
        {
            (byte) 0x01, // fastest algorithm (no compression)
            (byte) 0x5E, // fast algorithm (low compression)
            (byte) 0x9C, // default algorithm (default compression)
            (byte) 0xDA // slowest algorithm (maximum compression)
        });

        private static readonly IDictionary<DecompressionMethods, string> ContentEncodings = new Dictionary<DecompressionMethods, string>
        {
            {DecompressionMethods.GZip, "gzip"},
            {DecompressionMethods.Deflate, "deflate"}
        };

        public DecompressingHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        public DecompressionMethods AutomaticDecompression { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // add content encodings to the request
            var acceptEncoding = ContentEncodings
                .Where(p => AutomaticDecompression.HasFlag(p.Key))
                .Where(p => request.Headers.AcceptEncoding.All(c => c.Value.Equals(p.Value, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(p => p.Key)
                .Select(p => p.Value);

            foreach (var encoding in acceptEncoding)
            {
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue(encoding));
            }

            // get the response
            var response = await base.SendAsync(request, cancellationToken);

            // immediately return when the response body is empty or not compressed
            if (response.Content == null || !response.Content.Headers.ContentEncoding.Any())
            {
                return response;
            }

            // read the header
            var responseStream = await response.Content.ReadAsStreamAsync();
            var header = new byte[2];
            var headerLength = await responseStream.ReadAsync(header, 0, 2, cancellationToken);
            // TODO: handle headerLength == 1
            if (headerLength == 0)
            {
                return response;
            }

            // include the header in the stream that gets decompressed
            var chainedStream = new ChainedStream(new[] {new MemoryStream(header), responseStream});

            // try to decode the response
            Stream decodeStream = null;
            var contentEncoding = response.Content.Headers.ContentEncoding.First();
            if (contentEncoding.Equals(ContentEncodings[DecompressionMethods.GZip], StringComparison.OrdinalIgnoreCase))
            {
                // let gzip compress the stream, and exclude the Content-Encoding header
                decodeStream = new GZipStream(chainedStream, CompressionMode.Decompress);
            }
            else if (contentEncoding.Equals(ContentEncodings[DecompressionMethods.Deflate], StringComparison.OrdinalIgnoreCase))
            {
                // decompress the stream as raw DEFLATE or zlib, depending on the header
                var noHeader = header[0] != ValidZlibCmfByte || !ValidZlibFlgBytes.Contains(header[1]);
                decodeStream = noHeader ? new DeflateStream(chainedStream, CompressionMode.Decompress) : new DeflateStream(responseStream, CompressionMode.Decompress);
            }

            if (decodeStream != null)
            {
                var decompressedContent = new StreamContent(decodeStream);
                foreach (var pair in response.Content.Headers)
                {
                    if (pair.Key.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    decompressedContent.Headers.Add(pair.Key, pair.Value);
                }

                response.Content = decompressedContent;
            }

            return response;
        }
    }
}