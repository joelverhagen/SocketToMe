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
            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-request", It.Is<Stream>(s => ts.MatchingStream("serialized", s)), ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            ts.Serializer.Verify(x => x.Serialize(It.IsAny<MemoryStream>(), It.Is<HttpRequest>(m => m == ts.Request)), Times.Once);
            ts.Serializer.Verify(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpResponseOrException>()), Times.Never);

            ts.RequestMessage.Content.Should().NotBe(originalContent);
            (await ts.RequestMessage.Content.ReadAsStringAsync()).Should().Be("store-request-content");
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
            
            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-request", It.Is<Stream>(s => ts.MatchingStream("serialized", s)), ts.CancellationToken), Times.Once);
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
            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-response", It.Is<Stream>(s => ts.MatchingStream("serialized", s)), ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            ts.Serializer.Verify(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpRequest>()), Times.Never);
            ts.Serializer.Verify(x => x.Serialize(It.IsAny<MemoryStream>(), It.Is<HttpResponseOrException>(m => m.Response == ts.Response && m.ExceptionString == null)), Times.Once);

            ts.ResponseMessage.Content.Should().NotBe(originalContent);
            (await ts.ResponseMessage.Content.ReadAsStringAsync()).Should().Be("store-response-content");
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

            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-response", It.Is<Stream>(s => ts.MatchingStream("serialized", s)), ts.CancellationToken), Times.Once);
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

            ts.Store.Verify(x => x.SetAsync($"{ts.ExchangeId:N}-response", It.Is<Stream>(s => ts.MatchingStream("serialized", s)), ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            ts.Serializer.Verify(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpRequest>()), Times.Never);
            ts.Serializer.Verify(x => x.Serialize(It.IsAny<MemoryStream>(), It.Is<HttpResponseOrException>(m => m.Response == null && m.ExceptionString == ts.Exception.ToString())), Times.Once);
        }

        [Fact]
        public async Task GetsRequestsWithContent()
        {
            // Arrange
            var ts = new TestState();

            // Act
            var actual = await ts.Target.GetRequestAsync(ts.ExchangeId, ts.CancellationToken);

            // Assert
            ts.Store.Verify(x => x.GetAsync($"{ts.ExchangeId:N}-request", ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.GetAsync($"{ts.ExchangeId:N}-request-content", ts.CancellationToken), Times.Once);

            ts.Serializer.Verify(x => x.Deserialize<HttpRequest>(It.Is<Stream>(s => ts.MatchingStream("store-request", s))), Times.Once);

            ts.Mapper.Verify(x => x.ToHttpMessage(ts.Request), Times.Once);

            ts.ReadStream(ts.Request.Content).Should().Be("store-request-content");
            actual.Should().BeSameAs(ts.RequestMessage);
        }

        [Fact]
        public async Task GetsRequestsWithoutContent()
        {
            // Arrange
            var ts = new TestState();
            ts.Request.HasContent = false;
            ts.Request.Content = null;

            // Act
            var actual = await ts.Target.GetRequestAsync(ts.ExchangeId, ts.CancellationToken);

            // Assert
            ts.Store.Verify(x => x.GetAsync($"{ts.ExchangeId:N}-request", ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            ts.Serializer.Verify(x => x.Deserialize<HttpRequest>(It.Is<Stream>(s => ts.MatchingStream("store-request", s))), Times.Once);

            ts.Mapper.Verify(x => x.ToHttpMessage(ts.Request), Times.Once);

            ts.Request.Content.Should().BeNull();
            actual.Should().BeSameAs(ts.RequestMessage);
        }

        [Fact]
        public async Task ReturnsNullWhenRequestDoesNotExist()
        {
            // Arrange
            var ts = new TestState().WithMissingStoreEntry();

            // Act
            var actual = await ts.Target.GetRequestAsync(ts.ExchangeId, ts.CancellationToken);

            // Assert
            ts.Store.Verify(x => x.GetAsync($"{ts.ExchangeId:N}-request", ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            ts.Serializer.Verify(x => x.Deserialize<HttpRequest>(It.IsAny<Stream>()), Times.Never);

            ts.Mapper.Verify(x => x.ToHttpMessage(It.IsAny<HttpRequest>()), Times.Never);
            
            actual.Should().BeNull();
        }

        [Fact]
        public async Task GetsResponseWithContent()
        {
            // Arrange
            var ts = new TestState();

            // Act
            var actual = await ts.Target.GetResponseOrExceptionAsync(ts.ExchangeId, ts.CancellationToken);

            // Assert
            ts.Store.Verify(x => x.GetAsync($"{ts.ExchangeId:N}-response", ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.GetAsync($"{ts.ExchangeId:N}-response-content", ts.CancellationToken), Times.Once);

            ts.Serializer.Verify(x => x.Deserialize<HttpResponseOrException>(It.Is<Stream>(s => ts.MatchingStream("store-response", s))), Times.Once);

            ts.Mapper.Verify(x => x.ToHttpMessage(ts.Response), Times.Once);

            ts.ReadStream(ts.Response.Content).Should().Be("store-response-content");
            actual.Response.Should().BeSameAs(ts.ResponseMessage);
            actual.ExceptionString.Should().BeNull();
        }

        [Fact]
        public async Task GetsResponseWithoutContent()
        {
            // Arrange
            var ts = new TestState();
            ts.Response.HasContent = false;
            ts.Response.Content = null;

            // Act
            var actual = await ts.Target.GetResponseOrExceptionAsync(ts.ExchangeId, ts.CancellationToken);

            // Assert
            ts.Store.Verify(x => x.GetAsync($"{ts.ExchangeId:N}-response", ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            ts.Serializer.Verify(x => x.Deserialize<HttpResponseOrException>(It.Is<Stream>(s => ts.MatchingStream("store-response", s))), Times.Once);

            ts.Mapper.Verify(x => x.ToHttpMessage(ts.Response), Times.Once);

            ts.Response.Content.Should().BeNull();
            actual.Response.Should().BeSameAs(ts.ResponseMessage);
            actual.ExceptionString.Should().BeNull();
        }

        [Fact]
        public async Task GetsResponseWithException()
        {
            // Arrange
            var ts = new TestState();
            ts.ResponseOrException.Response = null;
            ts.ResponseOrException.ExceptionString = ts.Exception.ToString();

            // Act
            var actual = await ts.Target.GetResponseOrExceptionAsync(ts.ExchangeId, ts.CancellationToken);

            // Assert
            ts.Store.Verify(x => x.GetAsync($"{ts.ExchangeId:N}-response", ts.CancellationToken), Times.Once);
            ts.Store.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            ts.Serializer.Verify(x => x.Deserialize<HttpResponseOrException>(It.Is<Stream>(s => ts.MatchingStream("store-response", s))), Times.Once);

            ts.Mapper.Verify(x => x.ToHttpMessage(It.IsAny<HttpResponse>()), Times.Never);

            actual.Response.Should().BeNull();
            actual.ExceptionString.Should().Be(ts.Exception.ToString());
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
                Exception = new Exception("some exception");
                ResponseOrException = new HttpResponseOrException {Response = Response, ExceptionString = null};

                // setup
                Store
                    .Setup(x => x.GetAsync(It.Is<string>(k => k.EndsWith("request")), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(GetStream("store-request")));
                Store
                    .Setup(x => x.GetAsync(It.Is<string>(k => k.EndsWith("request-content")), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(GetStream("store-request-content")));
                Store
                    .Setup(x => x.GetAsync(It.Is<string>(k => k.EndsWith("response")), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(GetStream("store-response")));
                Store
                    .Setup(x => x.GetAsync(It.Is<string>(k => k.EndsWith("response-content")), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(GetStream("store-response-content")));
                Mapper
                    .Setup(x => x.ToHttpAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(Request));
                Mapper
                    .Setup(x => x.ToHttpMessage(It.IsAny<HttpRequest>()))
                    .Returns(() => RequestMessage);
                Mapper
                    .Setup(x => x.ToHttpAsync(It.IsAny<HttpResponseMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(Response));
                Mapper
                    .Setup(x => x.ToHttpMessage(It.IsAny<HttpResponse>()))
                    .Returns(() => ResponseMessage);
                Serializer
                    .Setup(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpRequest>()))
                    .Callback<Stream, HttpRequest>((s, r) => WriteToStream(s, "serialized"));
                Serializer
                    .Setup(x => x.Deserialize<HttpRequest>(It.IsAny<Stream>()))
                    .Returns(() => Request);
                Serializer
                    .Setup(x => x.Serialize(It.IsAny<Stream>(), It.IsAny<HttpResponseOrException>()))
                    .Callback<Stream, HttpResponseOrException>((s, r) => WriteToStream(s, "serialized"));
                Serializer
                    .Setup(x => x.Deserialize<HttpResponseOrException>(It.IsAny<Stream>()))
                    .Returns(() => ResponseOrException);

                Target = new HttpMessageStore(Store.Object, Mapper.Object, Serializer.Object);
            }

            public HttpResponseOrException ResponseOrException { get; set; }
            public Exception Exception { get; set; }
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

            public TestState WithMissingStoreEntry()
            {
                Store
                    .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(null);
                return this;
            }

            public bool MatchingStream(string expected, Stream stream)
            {
                return ReadStream(stream) == expected;
            }

            public string ReadStream(Stream stream)
            {
                var memoryStream = stream.Should().BeOfType<MemoryStream>().Which;
                memoryStream.Seek(0, SeekOrigin.Begin);

                var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }

            private Stream GetStream(string content)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(content));
            }

            private void WriteToStream(Stream stream, string content)
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                stream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
