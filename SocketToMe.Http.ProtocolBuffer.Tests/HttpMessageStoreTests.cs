using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Knapcode.SocketToMe.Http.ProtocolBuffer.Tests
{
    public class HttpMessageStoreTests
    {
        [Fact]
        public async Task StoresRequestsWithContent()
        {
            // Arrange
            var ts = new TestState();
            var originalContent = ts.RequestMessage.Content;

            // Act
            await ts.Target.SetAsync(ts.ExchangeId, ts.RequestMessage, ts.CancellationToken);

            // Assert
            ts.Mapper.Verify(x => x.ToHttpAsync(ts.RequestMessage, ts.CancellationToken), Times.Once);

            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-request-content", ts.Request.Content, ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.GetAsync($"{ts.ExchangeId:N}-request-content", ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-request", It.Is<Stream>(s => ts.MatchingStream(ts.SerializedContent, s)), ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            ts.Serializer.Verify(x => x.Serialize(It.IsAny<MemoryStream>(), It.Is<HttpRequest>(m => m == ts.Request)), Times.Once);
            ts.Serializer.Verify(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpResponseOrException>()), Times.Never);

            ts.RequestMessage.Content.Should().NotBe(originalContent);
            (await ts.RequestMessage.Content.ReadAsStringAsync()).Should().Be("store-request");
            ts.RequestMessage.Content.Headers.Should().HaveCount(2);
            ts.RequestMessage.Content.Headers.ContentType.ToString().Should().Be("text/plain");
            ts.RequestMessage.Content.Headers.ContentLength.Should().Be(7);
        }

        [Fact]
        public async Task StoresRequestsWithoutContent()
        {
            // Arrange
            var ts = new TestState();
            ts.RequestMessage.Content = null;
            ts.Request.HasContent = false;

            // Act
            await ts.Target.SetAsync(ts.ExchangeId, ts.RequestMessage, ts.CancellationToken);

            // Assert
            ts.Mapper.Verify(x => x.ToHttpAsync(ts.RequestMessage, ts.CancellationToken), Times.Once);
            
            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-request", It.Is<Stream>(s => ts.MatchingStream(ts.SerializedContent, s)), ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            ts.Serializer.Verify(x => x.Serialize(It.IsAny<MemoryStream>(), It.Is<HttpRequest>(m => m == ts.Request)), Times.Once);
            ts.Serializer.Verify(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpResponseOrException>()), Times.Never);

            ts.RequestMessage.Content.Should().BeNull();
        }

        [Fact]
        public async Task StoresResponsesWithContent()
        {
            // Arrange
            var ts = new TestState();
            var originalContent = ts.ResponseMessage.Content;

            // Act
            await ts.Target.SetAsync(ts.ExchangeId, ts.ResponseMessage, ts.CancellationToken);

            // Assert
            ts.Mapper.Verify(x => x.ToHttpAsync(ts.ResponseMessage, ts.CancellationToken), Times.Once);

            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-response-content", ts.Response.Content, ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.GetAsync($"{ts.ExchangeId:N}-response-content", ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-response", It.Is<Stream>(s => ts.MatchingStream(ts.SerializedContent, s)), ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            ts.Serializer.Verify(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpRequest>()), Times.Never);
            ts.Serializer.Verify(x => x.Serialize(It.IsAny<MemoryStream>(), It.Is<HttpResponseOrException>(m => m.Response == ts.Response && m.ExceptionString == null)), Times.Once);

            ts.ResponseMessage.Content.Should().NotBe(originalContent);
            (await ts.ResponseMessage.Content.ReadAsStringAsync()).Should().Be("store-response");
            ts.ResponseMessage.Content.Headers.Should().HaveCount(2);
            ts.ResponseMessage.Content.Headers.ContentType.ToString().Should().Be("text/plain");
            ts.ResponseMessage.Content.Headers.ContentLength.Should().Be(8);
        }

        [Fact]
        public async Task StoresResponsesWithoutContent()
        {
            // Arrange
            var ts = new TestState();
            ts.ResponseMessage.Content = null;
            ts.Response.HasContent = false;

            // Act
            await ts.Target.SetAsync(ts.ExchangeId, ts.ResponseMessage, ts.CancellationToken);

            // Assert
            ts.Mapper.Verify(x => x.ToHttpAsync(ts.ResponseMessage, ts.CancellationToken), Times.Once);

            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-response", It.Is<Stream>(s => ts.MatchingStream(ts.SerializedContent, s)), ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            ts.Serializer.Verify(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpRequest>()), Times.Never);
            ts.Serializer.Verify(x => x.Serialize(It.IsAny<MemoryStream>(), It.Is<HttpResponseOrException>(m => m.Response == ts.Response && m.ExceptionString == null)), Times.Once);

            ts.ResponseMessage.Content.Should().BeNull();
        }

        [Fact]
        public async Task StoresExceptions()
        {
            // Arrange
            var ts = new TestState();

            // Act
            await ts.Target.SetAsync(ts.ExchangeId, ts.Exception, ts.CancellationToken);

            // Assert
            ts.Mapper.Verify(x => x.ToHttpAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            ts.Mapper.Verify(x => x.ToHttpAsync(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()), Times.Never);

            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-response", It.Is<Stream>(s => ts.MatchingStream(ts.SerializedContent, s)), ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            ts.Serializer.Verify(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpRequest>()), Times.Never);
            ts.Serializer.Verify(x => x.Serialize(It.IsAny<MemoryStream>(), It.Is<HttpResponseOrException>(m => m.Response == null && m.ExceptionString == ts.Exception.ToString())), Times.Once);
        }

        private class TestState
        {
            public TestState()
            {
                // dependencies
                Store = new Mock<IStore>();
                Mapper = new Mock<IHttpMessageMapper>();
                Serializer = new Mock<IProtocolBufferSerializer>();

                // data
                CancellationToken = new CancellationToken();
                ExchangeId = new Guid("4ae0fbbaaeba4db1bd98d106ece1a6e0");
                RequestMessage = new HttpRequestMessage
                {
                    Content = new StreamContent(Stream.Null)
                    {
                        Headers =
                        {
                            { "Content-Type", "text/plain" },
                            { "Content-Length", "7" }
                        }
                    }
                };
                Request = new HttpRequest
                {
                    Method = "POST",
                    Url = "http://example/path",
                    Version = "1.1",
                    Headers = new[]
                    {
                        new HttpHeader {Name = "User-Agent", Value = "HttpMessageStore"},
                        new HttpHeader {Name = "Content-Type", Value = "text/plain"},
                        new HttpHeader {Name = "Content-Length", Value = "1"}
                    },
                    HasContent = true,
                    Content = GetStream("original-request")
                };
                ResponseMessage = new HttpResponseMessage
                {
                    Content = new StreamContent(Stream.Null)
                    {
                        Headers =
                        {
                            { "Content-Type", "text/plain" },
                            { "Content-Length", "8" }
                        }
                    }
                };
                Response = new HttpResponse
                {
                    Version = "1.1",
                    StatusCode = 200,
                    ReasonPhrease = "OK",
                    Headers = new[]
                    {
                        new HttpHeader {Name = "Server", Value = "HttpMessageStore"},
                        new HttpHeader {Name = "Content-Type", Value = "text/plain"},
                        new HttpHeader {Name = "Content-Length", Value = "2"}
                    },
                    HasContent = true,
                    Content = GetStream("original-response")
                };
                SerializedContent = "serialized";
                Exception = new Exception("some exception");

                // setup
                Store
                    .Setup(x => x.GetAsync(It.Is<string>(k => k.Contains("request-content")), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(GetStream("store-request")));
                Store
                    .Setup(x => x.GetAsync(It.Is<string>(k => k.Contains("response-content")), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(GetStream("store-response")));
                Mapper
                    .Setup(x => x.ToHttpAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(Request));
                Mapper
                    .Setup(x => x.ToHttpAsync(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(Response));
                Serializer
                    .Setup(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpRequest>()))
                    .Callback<Stream, HttpRequest>((s, r) => WriteToStream(s, SerializedContent));
                Serializer
                    .Setup(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpResponseOrException>()))
                    .Callback<Stream, HttpResponseOrException>((s, r) => WriteToStream(s, SerializedContent));

                Target = new HttpMessageStore(Store.Object, Mapper.Object, Serializer.Object);
            }

            public Exception Exception { get; set; }
            public string SerializedContent { get; set; }
            public Mock<IProtocolBufferSerializer> Serializer { get; set; }
            public CancellationToken CancellationToken { get; set; }
            public HttpResponseMessage ResponseMessage { get; set; }
            public HttpRequestMessage RequestMessage { get; set; }
            public Guid ExchangeId { get; set; }
            public HttpResponse Response { get; set; }
            public HttpMessageStore Target { get; set; }
            public Mock<IHttpMessageMapper> Mapper { get; set; }
            public HttpRequest Request { get; set; }
            public Mock<IStore> Store { get; set; }

            private Stream GetStream(string content)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(content));
            }

            public bool MatchingStream(string expected, Stream stream)
            {
                var memoryStream = stream.Should().BeOfType<MemoryStream>().Which;
                memoryStream.Seek(0, SeekOrigin.Begin);

                var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd() == expected;
            }

            private void WriteToStream(Stream stream, string content)
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                stream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
