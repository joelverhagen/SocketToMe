using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Knapcode.SocketToMe.Tests.Http
{
    [TestClass]
    public class NetworkHandlerTests
    {
        [TestMethod]
        public async Task BasicFunctionality()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/ip");

            // ACT
            var o = await ts.GetJsonResponse<IDictionary<string, string>>(request);

            // ASSERT
            o.Response.Should().NotBeNull();

            o.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            o.Response.ReasonPhrase.Should().Be("OK");
            o.Response.Version.ToString().Should().Be("1.1");

            o.Response.Headers.Should().NotBeNull();
            o.Response.Headers.Date.Should().HaveValue();
            o.Response.Headers.Date.Should().BeAfter(DateTime.MinValue);
            
            o.Content.Should().NotBeNull();
            o.Response.Content.Headers.ContentType.ToString().Should().Be("application/json");
            o.Content.Should().HaveCount(1);
            o.Content.Should().ContainKey("origin");
            IPAddress.Parse(o.Content["origin"]);
        }

        [TestMethod]
        public async Task Post()
        {
            // ARRANGE
            var ts = new TestState();
            var form = new Dictionary<string, string>
            {
                { "foo", "7A0D6A40-8DCE-4F6F-B372-ADC12B7FB222" },
                {"bar", "9C025D9D-9880-4C03-A251-A29D5EC4BF16" }
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "http://httpbin.org/post")
            {
                Content = new FormUrlEncodedContent(form)
            };

            // ACT
            var o = await ts.GetJsonResponse<JObject>(request);

            // ASSERT
            o.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            o.Content.Should().ContainKey("form");
            o.Content["form"].ToObject<IDictionary<string, string>>().ShouldBeEquivalentTo(form);
        }

        [TestMethod]
        public async Task IpAddressDestination()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://54.175.219.8/ip");
            request.Headers.Host = "httpbin.org";

            // ACT
            var o = await ts.GetJsonResponse<IDictionary<string, string>>(request);

            // ASSERT
            o.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            IPAddress.Parse(o.Content["origin"]);
        }

        [TestMethod]
        public async Task Headers()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/headers");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Foo", "Bar"));
            request.Headers.Add("x-sockettome-test", new[] {"ABBF9526-C07F-45A6-BE6C-2BE7E7B616F6", "CE8E3A72-9D2A-4284-966E-124713DE967F"});
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

        [TestMethod]
        public async Task StatusCode()
        {
            // ARRANGE
            var ts = new TestState();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://httpbin.org/status/409");

            // ACT
            var response = await ts.Client.SendAsync(request);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            response.ReasonPhrase.Should().Be("Conflict");
        }

        [TestMethod]
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

        private class TestState
        {
            public TestState()
            {
                // setup
                Handler = new NetworkHandler();
                Client = new HttpClient(Handler);
            }

            public HttpClient Client { get; set; }

            public NetworkHandler Handler { get; set; }

            public async Task<ResponseAndContent<T>> GetJsonResponse<T>(HttpRequestMessage request)
            {
                var response = await this.Client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                return new ResponseAndContent<T>
                {
                    Response = response,
                    Content = JsonConvert.DeserializeObject<T>(json)
                };
            }
        }

        private class ResponseAndContent<T>
        {
            public HttpResponseMessage Response { get; set; }
            public T Content { get; set; }
        }
    }
}
