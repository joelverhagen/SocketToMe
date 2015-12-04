using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Support;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Support
{
    public class LimitedStreamTests
    {
        [Fact]
        public void Limits_Synchronously()
        {
            // ARRANGE
            var innerStream = new MemoryStream(Encoding.ASCII.GetBytes("foobar"));
            var stream = new LimitedStream(innerStream, 3);

            // ACT
            var actual = new StreamReader(stream, Encoding.ASCII).ReadToEnd();

            // ASSERT
            actual.Should().Be("foo");
        }

        [Fact]
        public async Task Limits_Asynchronously()
        {
            // ARRANGE
            var innerStream = new MemoryStream(Encoding.ASCII.GetBytes("foobar"));
            var stream = new LimitedStream(innerStream, 3);

            // ACT
            var actual = await new StreamReader(stream, Encoding.ASCII).ReadToEndAsync();

            // ASSERT
            actual.Should().Be("foo");
        }
    }
}
