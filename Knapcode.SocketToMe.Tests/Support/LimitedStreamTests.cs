using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knapcode.SocketToMe.Tests.Support
{
    [TestClass]
    public class LimitedStreamTests
    {
        [TestMethod]
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

        [TestMethod]
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
