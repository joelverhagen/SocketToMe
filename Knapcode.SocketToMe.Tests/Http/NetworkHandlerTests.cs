using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Http
{
    public class NetworkHandlerTests
    {
        [Fact]
        public async Task DisposeResponseWithNoResponseBody()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Head, "http://httpbin.org/ip");
            var response = await ts.Client.SendAsync(request);

            // ACT
            Action action = () => response.Dispose();

            // ACT, ASSERT
            action.ShouldNotThrow();
        }

        [Fact]
        public async Task DisposeResponseWithContentLengthBody()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/ip");
            var response = await ts.Client.SendAsync(request);

            // ACT
            Action action = () => response.Dispose();

            // ACT, ASSERT
            action.ShouldNotThrow();
        }

        [Fact]
        public async Task DisposeResponseWithChunkedBody()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/stream/2");
            var response = await ts.Client.SendAsync(request);

            // ACT
            Action action = () => response.Dispose();

            // ACT, ASSERT
            action.ShouldNotThrow();
        }

        [Fact]
        public async Task Cookies()
        {
            // ARRANGE
            var ts = new TestState { CookieHandler = new CookieHandler(), RedirectingHandler = new RedirectingHandler() };

            // ACT
            await ts.GetJsonResponse<JObject>(new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/cookies/set?a=1&b=2A"));
            await ts.GetJsonResponse<JObject>(new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/cookies/set?b=2B"));
            await ts.GetJsonResponse<JObject>(new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/cookies/set?c=3"));
            await ts.GetJsonResponse<JObject>(new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/cookies/delete?c"));
            await ts.GetJsonResponse<JObject>(new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/cookies/delete?d"));
            var response = await ts.GetJsonResponse<JObject>(new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/cookies"));

            // ASSERT
            var cookies = response.Content["cookies"].ToObject<IDictionary<string, string>>();
            cookies.ShouldBeEquivalentTo(new Dictionary<string, string>
            {
                {"a", "1"},
                {"b", "2B"}
            });
        }

        [Fact]
        public async Task Gzip()
        {
            // ARRANGE
            var ts = new TestState { DecompressingHandler = new DecompressingHandler { AutomaticDecompression = DecompressionMethods.GZip } };
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/gzip");

            // ACT
            var response = await ts.GetJsonResponse<JObject>(request);

            // ASSERT
            response.Message.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content["gzipped"].ToObject<bool>().Should().BeTrue();
        }

        [Fact]
        public async Task Deflate()
        {
            // ARRANGE
            var ts = new TestState { DecompressingHandler = new DecompressingHandler { AutomaticDecompression = DecompressionMethods.Deflate } };
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/deflate");

            // ACT
            var response = await ts.GetJsonResponse<JObject>(request);

            // ASSERT
            response.Message.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content["deflated"].ToObject<bool>().Should().BeTrue();
        }

        [Fact]
        public async Task Redirect()
        {
            // ARRANGE
            var ts = new TestState { RedirectingHandler = new RedirectingHandler() };
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/redirect/10");

            // ACT
            var response = await ts.GetJsonResponse<JObject>(request);

            // ASSERT
            response.Message.RequestMessage.RequestUri.Should().Be(new Uri("http://httpbin.org/get"));
            response.Message.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CustomSocket()
        {
            // ARRANGE
            var ts = new TestState();
            ts.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ts.Socket.Connect("httpbin.org", 80);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/ip");

            // ACT
            var response = await ts.Client.SendAsync(request);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task CustomSocketDelegate()
        {
            // ARRANGE
            var ts = new TestState
            {
                GetSocket = r =>
                {
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect("httpbin.org", 80);
                    return socket;
                }
            };
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/ip");

            // ACT
            var response = await ts.Client.SendAsync(request);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task CustomSocketAsyncDelegate()
        {
            // ARRANGE
            var ts = new TestState
            {
                GetSocketAsync = r =>
                {
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect("httpbin.org", 80);
                    return Task.FromResult(socket);
                }
            };
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/ip");

            // ACT
            var response = await ts.Client.SendAsync(request);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task BasicFunctionality()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/ip");

            // ACT
            var o = await ts.GetJsonResponse<IDictionary<string, string>>(request);

            // ASSERT
            o.Message.Should().NotBeNull();

            o.Message.StatusCode.Should().Be(HttpStatusCode.OK);
            o.Message.ReasonPhrase.Should().Be("OK");
            o.Message.Version.ToString().Should().Be("1.1");

            o.Message.Headers.Should().NotBeNull();
            o.Message.Headers.Date.Should().HaveValue();
            o.Message.Headers.Date.Should().BeAfter(DateTime.MinValue);

            o.Content.Should().NotBeNull();
            o.Message.Content.Headers.ContentType.ToString().Should().Be("application/json");
            o.Content.Should().HaveCount(1);
            o.Content.Should().ContainKey("origin");
            IPAddress.Parse(o.Content["origin"]);
        }

        [Fact]
        public async Task KnownContentOverHttp()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/user-agent");
            request.Headers.Add("User-Agent", "SocketToMe/B3C5B340-D620-472E-B97B-769ECADD0CD3");

            // ACT
            var response = await ts.Client.SendAsync(request);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content = Regex.Replace(content, @"\s+", "");
            content.Should().Be(@"{""user-agent"":""SocketToMe/B3C5B340-D620-472E-B97B-769ECADD0CD3""}");
        }

        [Fact]
        public async Task KnownContentOverHttps()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://httpbin.org/user-agent");
            request.Headers.Add("User-Agent", "SocketToMe/B3C5B340-D620-472E-B97B-769ECADD0CD3");

            // ACT
            var response = await ts.Client.SendAsync(request);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content = Regex.Replace(content, @"\s+", "");
            content.Should().Be(@"{""user-agent"":""SocketToMe/B3C5B340-D620-472E-B97B-769ECADD0CD3""}");
        }

        [Fact]
        public async Task Post()
        {
            // ARRANGE
            var ts = new TestState();
            var form = new Dictionary<string, string>
            {
                {"foo", "7A0D6A40-8DCE-4F6F-B372-ADC12B7FB222"},
                {"bar", "9C025D9D-9880-4C03-A251-A29D5EC4BF16"}
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "http://httpbin.org/post")
            {
                Content = new FormUrlEncodedContent(form)
            };

            // ACT
            var o = await ts.GetJsonResponse<JObject>(request);

            // ASSERT
            o.Message.StatusCode.Should().Be(HttpStatusCode.OK);
            o.Content.Should().ContainKey("form");
            o.Content["form"].ToObject<IDictionary<string, string>>().ShouldBeEquivalentTo(form);
        }

        [Fact]
        public async Task IpAddressDestination()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://54.175.219.8/ip");
            request.Headers.Host = "httpbin.org";

            // ACT
            var o = await ts.GetJsonResponse<IDictionary<string, string>>(request);

            // ASSERT
            o.Message.StatusCode.Should().Be(HttpStatusCode.OK);
            IPAddress.Parse(o.Content["origin"]);
        }

        [Fact]
        public async Task Headers()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/headers");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Foo", "Bar"));
            request.Headers.Add("x-sockettome-test", new[] { "ABBF9526-C07F-45A6-BE6C-2BE7E7B616F6", "CE8E3A72-9D2A-4284-966E-124713DE967F" });
            var expectedHeaders = new Dictionary<string, string>
            {
                {"Host", "httpbin.org"},
                {"User-Agent", "Foo/Bar"},
                {"X-Sockettome-Test", "ABBF9526-C07F-45A6-BE6C-2BE7E7B616F6,CE8E3A72-9D2A-4284-966E-124713DE967F"}
            };

            // ACT
            var response = await ts.GetJsonResponse<JObject>(request);

            // ASSERT
            var headers = response.Content["headers"].ToObject<IDictionary<string, string>>();
            foreach (var header in expectedHeaders)
            {
                headers.Should().ContainKey(header.Key);
                headers[header.Key].Should().Be(header.Value);
            }
        }

        [Fact]
        public async Task RequestMessage()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/ip");

            // ACT
            var response = await ts.Client.SendAsync(request);

            // ASSERT
            response.RequestMessage.Should().BeSameAs(request);
        }

        [Fact]
        public async Task StatusCode()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/status/409");

            // ACT
            var response = await ts.Client.SendAsync(request);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            response.ReasonPhrase.Should().Be("CONFLICT");
        }

        [Fact]
        public async Task Head()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Head, "http://httpbin.org/ip");

            // ACT
            var response = await ts.Client.SendAsync(request);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ReasonPhrase.Should().Be("OK");
        }

        [Fact]
        public async Task ChunkedResponse()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/stream/2");
            request.Headers.Add("x-sockettome-test", new[] { "71116259-F72C-4B6C-8F83-764B787628BA" });

            // ACT
            var response = await ts.Client.SendAsync(request);

            // ASSERT
            response.Headers.TransferEncodingChunked.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            var lines = content.Trim().Split('\n');
            lines.Should().HaveCount(2);
            lines.Should().Contain(l => l.Contains("71116259-F72C-4B6C-8F83-764B787628BA"));
        }

        [Fact]
        public async Task Form()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Post, "http://httpbin.org/post");
            var expectedForm = new Dictionary<string, string>
            {
                { "foo", "9FEF809B-C5B6-4CDC-8F06-79C122BD71D5" },
                { "bar", "A400DB56-AFED-474B-A515-B371F29D78D9" }
            };
            request.Content = new FormUrlEncodedContent(expectedForm);

            // ACT
            var response = await ts.GetJsonResponse<JObject>(request);

            // ASSERT
            var actualForm = response.Content["form"].ToObject<IDictionary<string, string>>();
            actualForm.ShouldBeEquivalentTo(expectedForm);
        }

        [Fact]
        public async Task FileUpload()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Post, "http://httpbin.org/post");
            var content = new MultipartFormDataContent();

            var expectedForm = new Dictionary<string, string>
            {
                {"foo", "9FEF809B-C5B6-4CDC-8F06-79C122BD71D5"},
                {"bar", "A400DB56-AFED-474B-A515-B371F29D78D9"}
            };
            foreach (var pair in expectedForm)
            {
                content.Add(new StringContent(pair.Value), pair.Key);
            }

            var fileContent = new MemoryStream(Encoding.ASCII.GetBytes("This is the file content."));
            content.Add(new StreamContent(fileContent)
            {
                Headers =
                {
                    ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = "FileName.txt",
                        Name = "Name.txt"
                    }
                }
            });
            request.Content = content;

            // ACT
            var response = await ts.GetJsonResponse<JObject>(request);

            // ASSERT
            var actualForm = response.Content["form"].ToObject<IDictionary<string, string>>();
            actualForm.ShouldBeEquivalentTo(expectedForm);

            var actualFiles = response.Content["files"].ToObject<IDictionary<string, string>>();
            actualFiles.ShouldBeEquivalentTo(new Dictionary<string, string>
            {
                {"Name.txt", "This is the file content."}
            });
        }

        private class TestState
        {
            private HttpClient _client;

            public TestState()
            {
                // setup
                Socket = null;
                GetSocket = null;
                GetSocketAsync = null;
                DecompressingHandler = null;
                RedirectingHandler = null;
                CookieHandler = null;
            }

            public CookieHandler CookieHandler { get; set; }

            public GetSocketAsync GetSocketAsync { get; set; }

            public GetSocket GetSocket { get; set; }

            public RedirectingHandler RedirectingHandler { get; set; }

            public DecompressingHandler DecompressingHandler { get; set; }

            public Socket Socket { get; set; }

            public HttpClient Client
            {
                get
                {
                    if (_client == null)
                    {
                        HttpMessageHandler handler = Handler;

                        if (CookieHandler != null)
                        {
                            CookieHandler.InnerHandler = handler;
                            handler = CookieHandler;
                        }

                        if (RedirectingHandler != null)
                        {
                            RedirectingHandler.InnerHandler = handler;
                            handler = RedirectingHandler;
                        }

                        if (DecompressingHandler != null)
                        {
                            DecompressingHandler.InnerHandler = handler;
                            handler = DecompressingHandler;
                        }

                        _client = new HttpClient(handler);
                    }

                    return _client;
                }
            }

            public NetworkHandler Handler
            {
                get
                {
                    if (Socket != null)
                    {
                        return new NetworkHandler(Socket);
                    }

                    if (GetSocket != null)
                    {
                        return new NetworkHandler(GetSocket);
                    }

                    if (GetSocketAsync != null)
                    {
                        return new NetworkHandler(GetSocketAsync);
                    }

                    return new NetworkHandler();
                }
            }

            public async Task<ResponseAndContent<T>> GetJsonResponse<T>(HttpRequestMessage request)
            {
                var response = await Client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                return new ResponseAndContent<T>
                {
                    Message = response,
                    Content = JsonConvert.DeserializeObject<T>(json)
                };
            }
        }

        private class ResponseAndContent<T>
        {
            public HttpResponseMessage Message { get; set; }
            public T Content { get; set; }
        }
    }
}