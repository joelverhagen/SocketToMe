using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Knapcode.SocketToMe.Tests.Http
{
    [TestClass]
    public class CookieHandlerTests
    {
        [TestMethod, TestCategory("Unit")]
        public async Task NoCookies()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT
            await ts.HttpClient.SendAsync(ts.Request);

            // ASSERT
            ts.VerifyEmptyCookieContainer();
        }

        [TestMethod, TestCategory("Unit")]
        public async Task CookiesOnTheRequestButNotInTheContainer()
        {
            // ARRANGE
            var ts = new TestState
            {
                Request =
                {
                    Headers =
                    {
                        {"Cookie", "a=1; b=2"},
                        {"cookie", "c=3; "},
                        {"COOKIE", "d=4;"},
                        {"cOOKIE", "e=5"}
                    }
                }
            };

            // ACT
            await ts.HttpClient.SendAsync(ts.Request);

            // ASSERT
            ts.VerifyRequestCookie("a=1; b=2; c=3; d=4; e=5");
            ts.VerifyEmptyCookieContainer();
        }

        [TestMethod, TestCategory("Unit")]
        public async Task CookiesOnTheRequestAndInTheContainer()
        {
            // ARRANGE
            var ts = new TestState
            {
                Request =
                {
                    Headers =
                    {
                        {"Cookie", "a=1; b=2"},
                        {"cookie", "c=3; "},
                        {"COOKIE", "d=4;"},
                        {"cOOKIE", "e=5"}
                    }
                }
            };
            ts.CookieContainer.Add(ts.Uri, new Cookie("f", "6"));
            ts.CookieContainer.Add(ts.Uri, new Cookie("g", "7"));

            // ACT
            await ts.HttpClient.SendAsync(ts.Request);

            // ASSERT
            ts.VerifyRequestCookie("a=1; b=2; c=3; d=4; e=5; f=6; g=7");
        }

        [TestMethod, TestCategory("Unit")]
        public async Task CookiesInTheContainerButNotOnTheRequest()
        {
            // ARRANGE
            var ts = new TestState();
            ts.CookieContainer.Add(ts.Uri, new Cookie("a", "1"));
            ts.CookieContainer.Add(ts.Uri, new Cookie("b", "2"));

            // ACT
            await ts.HttpClient.SendAsync(ts.Request);

            // ASSERT
            ts.VerifyRequestCookie("a=1; b=2");
        }

        [TestMethod, TestCategory("Unit")]
        public async Task CookiesInTheContainerOnADifferentUri()
        {
            // ARRANGE
            var ts = new TestState();
            ts.CookieContainer.Add(new Uri("http://different-a"), new Cookie("a", "1"));
            ts.CookieContainer.Add(new Uri("http://different-b"), new Cookie("b", "2"));

            // ACT
            await ts.HttpClient.SendAsync(ts.Request);

            // ASSERT
            ts.Request.Headers.Where(h => string.Equals(h.Key, "Cookie", StringComparison.OrdinalIgnoreCase)).Should().BeEmpty();
        }

        [TestMethod, TestCategory("Unit")]
        public async Task SetCookies()
        {
            // ARRANGE
            var ts = new TestState();
            ts.CookieContainer.Add(ts.Uri, new Cookie("a", "1"));
            ts.CookieContainer.Add(ts.Uri, new Cookie("b", "2A"));
            ts.Response.Headers.Add("Set-Cookie", "a=1; Expires=Thu, 01-Jan-1970 00:00:00 GMT");
            ts.Response.Headers.Add("Set-Cookie", "b=2B");
            ts.Response.Headers.Add("Set-Cookie", "c=3");

            // ACT
            await ts.HttpClient.SendAsync(ts.Request);

            // ASSERT
            ts.CookieContainer.Count.Should().Be(2);
            var cookieCollection = ts.CookieContainer.GetCookies(ts.Uri);
            cookieCollection.Should().HaveCount(2);
            cookieCollection[0].Name.Should().Be("b");
            cookieCollection[0].Value.Should().Be("2B");
            cookieCollection[1].Name.Should().Be("c");
            cookieCollection[1].Value.Should().Be("3");
        }

        private class TestState
        {
            public TestState()
            {
                // data
                this.Uri = new Uri("http://example");
                this.Request = new HttpRequestMessage(HttpMethod.Get, this.Uri);
                this.Response = new HttpResponseMessage(HttpStatusCode.OK);
                this.CookieContainer = new CookieContainer();
                this.TestHandler = new TestHandler(this);

                // setup
                this.CookieHandler = new CookieHandler { CookieContainer = this.CookieContainer, InnerHandler = TestHandler };
                this.HttpClient = new HttpClient(this.CookieHandler);
            }

            public Uri Uri { get; set; }

            public HttpRequestMessage Request { get; set; }

            public HttpClient HttpClient { get; set; }

            public TestHandler TestHandler { get; set; }

            public CookieHandler CookieHandler { get; set; }

            public CookieContainer CookieContainer { get; set; }

            public HttpResponseMessage Response { get; set; }

            public void VerifyEmptyCookieContainer()
            {
                this.CookieContainer.Count.Should().Be(0);
                this.CookieContainer.GetCookies(this.Uri).Should().BeEmpty();
            }

            public void VerifyRequestCookie(string value)
            {
                this.Request.Headers.Contains("Cookie").Should().BeTrue();
                this.Request.Headers.Where(h => h.Key == "Cookie").ShouldBeEquivalentTo(new[] { new KeyValuePair<string, IEnumerable<string>>("Cookie", new[] { value }) });
            }
        }

        private class TestHandler : DelegatingHandler
        {
            private readonly TestState _testState;

            public TestHandler(TestState testState)
            {
                _testState = testState;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _testState.Response.RequestMessage = request;
                return Task.FromResult(_testState.Response);
            }
        }
    }
}
