using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Http;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Http.Mapping
{
    public class HttpMessageMapperTests
    {
        [Fact]
        public async Task ConvertsRequestToRequestMessage()
        {
            // ARRANGE
            var input = GetRequest();
            var mapper = new HttpMessageMapper();

            // ACT
            var output = mapper.ToHttpRequestMessage(input);

            // ASSERT
            output.Method.Should().Be(HttpMethod.Get);
            output.RequestUri.Should().Be(new Uri("http://example/path"));
            output.Version.Should().Be(new Version(1, 1));
            output.Headers.UserAgent.Should().HaveCount(1);
            output.Headers.UserAgent.ToString().Should().Be("HttpMessageMapper");
            output.Content.Should().NotBeNull();
            output.Content.Headers.ContentType.ToString().Should().Be("application/json");
            output.Content.Headers.ContentLength.Should().Be(99);
            (await output.Content.ReadAsStringAsync()).Should().Be("foobar");
            output.Properties.Should().BeEmpty();
        }

        [Fact]
        public async Task ConvertsResponseToResponseMessage()
        {
            // ARRANGE
            var input = GetResponse();
            var mapper = new HttpMessageMapper();

            // ACT
            var output = mapper.ToHttpResponseMessage(input);

            // ASSERT
            output.Version.Should().Be(new Version(1, 1));
            output.StatusCode.Should().Be(HttpStatusCode.NotFound);
            output.ReasonPhrase.Should().Be("Not Found");
            output.Headers.Server.Should().HaveCount(1);
            output.Headers.Server.ElementAt(0).ToString().Should().Be("HttpMessageMapper");
            output.Content.Should().NotBeNull();
            output.Content.Headers.ContentType.ToString().Should().Be("application/json");
            output.Content.Headers.ContentLength.Should().Be(99);
            (await output.Content.ReadAsStringAsync()).Should().Be("foobar");
            output.RequestMessage.Should().BeNull();
        }

        [Fact]
        public async Task ConvertsRequestMessageToRequest()
        {
            // ARRANGE
            var input = GetRequestMessage();
            var mapper = new HttpMessageMapper();

            // ACT
            var output = await mapper.ToHttpRequestAsync(input, CancellationToken.None);

            // ASSERT
            output.Method.Should().Be("GET");
            output.Url.Should().Be("http://example/path");
            output.Version.Should().Be("1.1");
            output.Headers.ShouldBeEquivalentTo(
                new[]
                {
                    new HttpHeader { Name = "User-Agent", Value = "HttpMessageMapper/2.0" },
                    new HttpHeader { Name = "Content-Type", Value = "application/json" },
                    new HttpHeader { Name = "Content-Length", Value = "99" }
                },
                o => o.WithStrictOrdering());
            (await new StreamReader(output.Content).ReadToEndAsync()).Should().Be("foobar");
        }

        [Fact]
        public async Task ConvertsResponseMessageToResponse()
        {
            // ARRANGE
            var input = GetResponseMessage();
            var mapper = new HttpMessageMapper();

            // ACT
            var output = await mapper.ToHttpResponseAsync(input, CancellationToken.None);

            // ASSERT
            output.Version.Should().Be("1.1");
            output.StatusCode.Should().Be(404);
            output.ReasonPhrease.Should().Be("Not Found");
            output.Headers.ShouldBeEquivalentTo(
                new[]
                {
                    new HttpHeader { Name = "Server", Value = "HttpMessageMapper/2.0" },
                    new HttpHeader { Name = "Content-Type", Value = "application/json" },
                    new HttpHeader { Name = "Content-Length", Value = "99" }
                },
                o => o.WithStrictOrdering());
            (await new StreamReader(output.Content).ReadToEndAsync()).Should().Be("foobar");
        }

        [Fact]
        public void AllowsNullContentOnRequest()
        {
            // ARRANGE
            var input = GetRequest();
            input.Content = null;
            var mapper = new HttpMessageMapper();

            // ACT
            var output = mapper.ToHttpRequestMessage(input);

            // ASSERT
            output.Content.Should().BeNull();
            output.Headers.Should().HaveCount(1);
            output.Headers.UserAgent.ToString().Should().Be("HttpMessageMapper");
        }

        [Fact]
        public void AllowsNullContentOnResponse()
        {
            // ARRANGE
            var input = GetResponse();
            input.Content = null;
            var mapper = new HttpMessageMapper();

            // ACT
            var output = mapper.ToHttpResponseMessage(input);

            // ASSERT
            output.Content.Should().BeNull();
            output.Headers.Should().HaveCount(1);
            output.Headers.Server.ToString().Should().Be("HttpMessageMapper");
        }

        [Fact]
        public async Task AllowsNullContentOnRequestMessage()
        {
            // ARRANGE
            var input = GetRequestMessage();
            input.Content = null;
            var mapper = new HttpMessageMapper();

            // ACT
            var output = await mapper.ToHttpRequestAsync(input, CancellationToken.None);

            // ASSERT
            output.Content.Should().BeNull();
            output.Headers.Should().HaveCount(1);
            output.Headers.ShouldBeEquivalentTo(
                new[] { new HttpHeader { Name = "User-Agent", Value = "HttpMessageMapper/2.0" } },
                o => o.WithStrictOrdering());
        }

        [Fact]
        public async Task AllowsNullContentOnResponseMessage()
        {
            // ARRANGE
            var input = GetResponseMessage();
            input.Content = null;
            var mapper = new HttpMessageMapper();

            // ACT
            var output = await mapper.ToHttpResponseAsync(input, CancellationToken.None);

            // ASSERT
            output.Content.Should().BeNull();
            output.Headers.Should().HaveCount(1);
            output.Headers.ShouldBeEquivalentTo(
                new[] { new HttpHeader { Name = "Server", Value = "HttpMessageMapper/2.0" } },
                o => o.WithStrictOrdering());
        }

        [Fact]
        public void AllowsNullHeadersOnRequest()
        {
            // ARRANGE
            var input = GetRequest();
            input.Headers = null;
            var mapper = new HttpMessageMapper();

            // ACT
            var output = mapper.ToHttpRequestMessage(input);

            // ASSERT
            output.Headers.Should().BeEmpty();
        }

        [Fact]
        public void AllowsNullHeadersOnResponse()
        {
            // ARRANGE
            var input = GetResponse();
            input.Headers = null;
            var mapper = new HttpMessageMapper();

            // ACT
            var output = mapper.ToHttpResponseMessage(input);

            // ASSERT
            output.Headers.Should().BeEmpty();
        }

        private static HttpRequest GetRequest()
        {
            return new HttpRequest
            {
                Method = "GET",
                Url = "http://example/path",
                Version = "1.1",
                Headers = new List<HttpHeader>
                {
                    new HttpHeader { Name = "User-Agent", Value = "HttpMessageMapper" },
                    new HttpHeader { Name = "Content-Type", Value = "application/json" },
                    new HttpHeader { Name = "Content-Length", Value = "99" }
                },
                Content = new MemoryStream(Encoding.ASCII.GetBytes("foobar"))
            };
        }

        private static HttpResponse GetResponse()
        {
            return new HttpResponse
            {
                Version = "1.1",
                StatusCode = 404,
                ReasonPhrease = "Not Found",
                Headers = new List<HttpHeader>
                {
                    new HttpHeader { Name = "Server", Value = "HttpMessageMapper" },
                    new HttpHeader { Name = "Content-Type", Value = "application/json" },
                    new HttpHeader { Name = "Content-Length", Value = "99" }
                },
                Content = new MemoryStream(Encoding.ASCII.GetBytes("foobar"))
            };
        }

        private static HttpRequestMessage GetRequestMessage()
        {
            return new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("http://example/path"),
                Version = new Version(1, 1),
                Headers =
                {
                    UserAgent = { new ProductInfoHeaderValue("HttpMessageMapper", "2.0") }
                },
                Content = new StringContent("foobar")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json"),
                        ContentLength = 99
                    }
                }
            };
        }

        private static HttpResponseMessage GetResponseMessage()
        {
            return new HttpResponseMessage
            {
                Version = new Version(1, 1),
                StatusCode = HttpStatusCode.NotFound,
                ReasonPhrase = "Not Found",
                Headers =
                {
                    Server = { new ProductInfoHeaderValue("HttpMessageMapper", "2.0") }
                },
                Content = new StringContent("foobar")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json"),
                        ContentLength = 99
                    }
                }
            };
        }
    }
}
