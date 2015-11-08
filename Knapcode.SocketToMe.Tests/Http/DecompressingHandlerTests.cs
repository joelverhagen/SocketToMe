using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Knapcode.SocketToMe.Http;
using Moq;
using Moq.Protected;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Http
{
    public class DecompressingHandlerTests
    {
        private const string OriginalContent = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Lorem ipsum dolor sit amet.";

        [Fact]
        public async Task SendAsync_WithBclRawDeflate_DecodesResponse()
        {
            await SendAsync_WithMockedInnerHandler_DecodesResponse(DecompressionMethods.Deflate, GetBclRawDeflateHandler);
        }

        [Fact]
        public async Task SendAsync_WithSharpZipLibRawDeflate_DecodesResponse()
        {
            await SendAsync_WithMockedInnerHandler_DecodesResponse(DecompressionMethods.Deflate, GetSharpZipLibRawDeflateHandler);
        }

        [Fact]
        public async Task SendAsync_WithSharpZipLibZlibDeflate_DecodesResponse()
        {
            await SendAsync_WithMockedInnerHandler_DecodesResponse(DecompressionMethods.Deflate, GetSharpZipLibZlibDeflateHandler);
        }

        [Fact]
        public async Task SendAsync_WithBclGzip_DecodesResponse()
        {
            await SendAsync_WithMockedInnerHandler_DecodesResponse(DecompressionMethods.GZip, GetBclGzipHandler);
        }

        [Fact]
        public async Task SendAsync_WithSharpZipLibGzip_DecodesResponse()
        {
            await SendAsync_WithMockedInnerHandler_DecodesResponse(DecompressionMethods.GZip, GetSharpZipLibGzipHandler);
        }

        [Fact]
        public async Task SendAsync_WithIdentity_ReturnsOriginalResponse()
        {
            await SendAsync_WithMockedInnerHandler_DecodesResponse(DecompressionMethods.None, GetIdentityHandler);
        }

        private static async Task SendAsync_WithMockedInnerHandler_DecodesResponse(DecompressionMethods decompressionMethods, Func<string, Mock<HttpMessageHandler>> getHandlerMock)
        {
            // ARRANGE
            var mock = getHandlerMock(OriginalContent);
            var pipeline = new DecompressingHandler
            {
                AutomaticDecompression = decompressionMethods,
                InnerHandler = mock.Object
            };
            var client = new HttpClient(pipeline);

            // ACT
            HttpResponseMessage response = await client.GetAsync("http://localhost");

            // ASSERT
            string actualContent = await response.Content.ReadAsStringAsync();
            response.Content.Headers.ContentEncoding.Should().BeEmpty();
            actualContent.Should().Be(OriginalContent);

            mock
                .Protected()
                .Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        public static Mock<HttpMessageHandler> GetBclRawDeflateHandler(string originalContent)
        {
            return GetHandler(
                "deflate",
                originalContent,
                b =>
                {
                    var responseStream = new MemoryStream();
                    using (var compressStream = new DeflateStream(responseStream, CompressionMode.Compress))
                    {
                        compressStream.Write(b, 0, b.Length);
                    }
                    return new MemoryStream(responseStream.ToArray());
                });
        }

        public static Mock<HttpMessageHandler> GetSharpZipLibRawDeflateHandler(string originalContent)
        {
            return GetHandler(
                "deflate",
                originalContent,
                b =>
                {
                    var responseStream = new MemoryStream();
                    using (var compressStream = new DeflaterOutputStream(responseStream, new Deflater(6, true)) {IsStreamOwner = false})
                    {
                        compressStream.Write(b, 0, b.Length);
                    }
                    responseStream.Position = 0;
                    return responseStream;
                });
        }

        public static Mock<HttpMessageHandler> GetSharpZipLibZlibDeflateHandler(string originalContent)
        {
            return GetHandler(
                "deflate",
                originalContent,
                b =>
                {
                    var responseStream = new MemoryStream();
                    using (var compressStream = new DeflaterOutputStream(responseStream, new Deflater(6, false)) {IsStreamOwner = false})
                    {
                        compressStream.Write(b, 0, b.Length);
                    }
                    responseStream.Position = 0;
                    return responseStream;
                });
        }

        public static Mock<HttpMessageHandler> GetBclGzipHandler(string originalContent)
        {
            return GetHandler(
                "gzip",
                originalContent,
                b =>
                {
                    var responseStream = new MemoryStream();
                    using(var compressStream = new GZipStream(responseStream, CompressionMode.Compress))
                    {
                        compressStream.Write(b, 0, b.Length);   
                    }
                    return new MemoryStream(responseStream.ToArray());
                });
        }

        public static Mock<HttpMessageHandler> GetSharpZipLibGzipHandler(string originalContent)
        {
            return GetHandler(
                "gzip",
                originalContent,
                b =>
                {
                    var responseStream = new MemoryStream();
                    using (var compressStream = new GZipOutputStream(responseStream) {IsStreamOwner = false})
                    {
                        compressStream.Write(b, 0, b.Length);
                    }
                    responseStream.Position = 0;
                    return responseStream;
                });
        }

        public static Mock<HttpMessageHandler> GetIdentityHandler(string originalContent)
        {
            return GetHandler(
                null,
                originalContent,
                b => new MemoryStream(b));
        }

        private static Mock<HttpMessageHandler> GetHandler(string contentEncoding, string originalContent, Func<byte[], Stream> getResponseStream)
        {
            var mock = new Mock<HttpMessageHandler>();

            mock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
                {
                    byte[] originalBytes = Encoding.UTF8.GetBytes(originalContent);
                    
                    var response = new HttpResponseMessage
                    {
                        Content = new StreamContent(getResponseStream(originalBytes))
                    };

                    if (contentEncoding != null)
                    {
                        response.Content.Headers.ContentEncoding.Add(contentEncoding);
                    }

                    return Task.FromResult(response);
                });

            return mock;
        }
    }
}
