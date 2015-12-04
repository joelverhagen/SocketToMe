using System;
using System.Collections.Generic;
using System.Net.Http;
using FluentAssertions;
using Knapcode.SocketToMe.Http;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Http
{
    public class ExtensionsForLoggingHandlerTests
    {
        [Fact]
        public void GetsExchangeIdWhenAvailable()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT
            var actual = ts.Request.GetExchangeId();

            // ASSERT
            actual.Should().Be(ts.ExchangeId);
        }

        [Fact]
        public void FailsToGetExchangeIdWhenUnavailable()
        {
            // ARRANGE
            var ts = new TestState().WithoutExchangeId();
            Action action = () => ts.Request.GetExchangeId();

            // ACT, ASSERT
            action.ShouldThrow<KeyNotFoundException>().Which.Message.Should().Be("The exchange ID could not be found on the request.");
        }

        [Fact]
        public void FailsToGetExchangeIdOfWrongType()
        {
            // ARRANGE
            var ts = new TestState().WithExchangeIdOfWrongType();
            Action action = () => ts.Request.GetExchangeId();

            // ACT, ASSERT
            action.ShouldThrow<InvalidOperationException>().Which.Message.Should().Be("The exchange ID found in the request is not a GUID.");
        }

        [Fact]
        public void TriesToGetExchangeIdWhenAvailable()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT
            var actual = ts.Request.TryGetExchangeId(out ts.OutExchangeId);

            // ASSERT
            actual.Should().Be(true);
            ts.OutExchangeId.Should().Be(ts.ExchangeId);
        }

        [Fact]
        public void TriesToGetExchangeIdWhenUnavailable()
        {
            // ARRANGE
            var ts = new TestState().WithoutExchangeId();

            // ACT
            var actual = ts.Request.TryGetExchangeId(out ts.OutExchangeId);

            // ASSERT
            actual.Should().Be(false);
            ts.OutExchangeId.Should().Be(default(Guid));
        }

        [Fact]
        public void TriesToGetExchangeIdOfWrongType()
        {
            // ARRANGE
            var ts = new TestState().WithExchangeIdOfWrongType();

            // ACT
            var actual = ts.Request.TryGetExchangeId(out ts.OutExchangeId);

            // ASSERT
            actual.Should().Be(false);
            ts.OutExchangeId.Should().Be(default(Guid));
        }

        private class TestState
        {
            public TestState()
            {
                // data
                Request = new HttpRequestMessage();
                ExchangeId = new Guid("E63B6D2C-BD08-4BCD-849E-67E929E1147D");

                // setup
                Request.Properties[LoggingHandler.ExchangeIdPropertyKey] = ExchangeId;
            }

            public Guid OutExchangeId;

            public Guid ExchangeId { get; set; }

            public HttpRequestMessage Request { get; set; }

            public TestState WithoutExchangeId()
            {
                Request.Properties.Remove(LoggingHandler.ExchangeIdPropertyKey);
                return this;
            }

            public TestState WithExchangeIdOfWrongType()
            {
                Request.Properties[LoggingHandler.ExchangeIdPropertyKey] = "bad";
                return this;
            }
        }
    }
}
