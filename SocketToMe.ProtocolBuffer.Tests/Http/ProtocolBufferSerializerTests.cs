using System.IO;
using FluentAssertions;
using Knapcode.SocketToMe.Http;
using Knapcode.SocketToMe.ProtocolBuffer.Http;
using Xunit;

namespace Knapcode.SocketToMe.ProtocolBuffer.Tests.Http
{
    public class ProtocolBufferSerializerTests
    {
        [Fact]
        public void SerializesAndDeserializesHttpRequest()
        {
            // ARRANGE
            var expected = new HttpRequest
            {
                Method = "POST",
                Url = "http://example/path",
                Version = "1.1",
                Headers = new[]
                {
                    new HttpHeader {Name = "User-Agent", Value = "ProtocolBufferSerializer"},
                    new HttpHeader {Name = "Content-Type", Value = "text/plain"}
                },
                HasContent = true,
                Content = new MemoryStream(new byte[0])
            };

            // ACT
            var actual = SerializeAndDeserialize(expected);

            // ASSERT
            actual.Should().NotBe(expected);
            actual.ShouldBeEquivalentTo(expected, o => o.Excluding(x => x.Content));
            actual.Content.Should().BeNull();
        }

        [Fact]
        public void SerializesAndDeserializesHttpResponseOrException()
        {
            // ARRANGE
            var expected = new HttpResponseOrException
            {
                Response = new HttpResponse
                {
                    Version = "1.1",
                    StatusCode = 200,
                    ReasonPhrease = "OK",
                    Headers = new[]
                    {
                        new HttpHeader {Name = "Server", Value = "ProtocolBufferSerializer"},
                        new HttpHeader {Name = "Content-Type", Value = "text/plain"}
                    },
                    HasContent = true,
                    Content = new MemoryStream(new byte[0])
                },
                ExceptionString = "exception"
            };

            // ACT
            var actual = SerializeAndDeserialize(expected);

            // ASSERT
            actual.Should().NotBe(expected);
            actual.ShouldBeEquivalentTo(expected, o => o.Excluding(x => x.Response.Content));
            actual.Response.Content.Should().BeNull();
        }

        private T SerializeAndDeserialize<T>(T input)
        {
            // ARRANGE
            var serializer = new ProtocolBufferSerializer();
            var memoryStream = new MemoryStream();
            
            // ACT
            serializer.Serialize(memoryStream, input);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return serializer.Deserialize<T>(memoryStream);
        }
    }
}
