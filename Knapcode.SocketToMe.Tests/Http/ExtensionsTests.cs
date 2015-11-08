using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FluentAssertions;
using Knapcode.SocketToMe.Http;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Http
{
    public class ExtensionsTests
    {
        private delegate bool TryGetHttpRequestMessageProperty<T>(HttpRequestMessage request, out T value);

        [Fact]
        public void TryGetRedirectHistory_WithValue_ReturnsTrue()
        {
            TryGetHttpRequestMessageProperty_WithValue_ReturnsTrue(Extensions.TryGetRedirectHistory, RedirectingHandler.RedirectHistoryKey, Enumerable.Empty<HttpResponseMessage>());
        }

        [Fact]
        public void TryGetRedirectHistory_WithWrongType_ReturnsFalse()
        {
            TryGetHttpRequestMessageProperty_WithWrongType_ReturnsFalse<IEnumerable<HttpResponseMessage>>(Extensions.TryGetRedirectHistory, RedirectingHandler.RedirectHistoryKey);
        }

        [Fact]
        public void TryGetRedirectHistory_WithNoValue_ReturnsFalse()
        {
            TryGetHttpRequestMessageProperty_WithNoValue_ReturnsFalse<IEnumerable<HttpResponseMessage>>(Extensions.TryGetRedirectHistory);
        }

        private static void TryGetHttpRequestMessageProperty_WithValue_ReturnsTrue<T>(TryGetHttpRequestMessageProperty<T> get, string key, T expected, object addedValue = null) where T : class
        {
            // ARRANGE
            if (addedValue == null)
            {
                addedValue = expected;
            }

            var request = new HttpRequestMessage();
            request.Properties.Add(key, addedValue);
            T actual;

            // ACT
            bool success = get(request, out actual);

            // ASSERT
            success.Should().BeTrue();
            actual.Should().BeSameAs(expected);
        }

        private static void TryGetHttpRequestMessageProperty_WithWrongType_ReturnsFalse<T>(TryGetHttpRequestMessageProperty<T> get, string key)
        {
            // ARRANGE
            var request = new HttpRequestMessage();
            request.Properties.Add(key, typeof(T) == typeof(string) ? (object)23 : "23");
            T actual;

            // ACT
            bool success = get(request, out actual);

            // ASSERT
            success.Should().BeFalse();
            actual.Should().Be(default(T));
        }

        private static void TryGetHttpRequestMessageProperty_WithNoValue_ReturnsFalse<T>(TryGetHttpRequestMessageProperty<T> get)
        {
            // ARRANGE
            var request = new HttpRequestMessage();
            T actual;

            // ACT
            bool success = get(request, out actual);

            // ASSERT
            success.Should().BeFalse();
            actual.Should().Be(default(T));
        }

    }
}
