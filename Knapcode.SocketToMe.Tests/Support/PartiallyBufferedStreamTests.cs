using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knapcode.SocketToMe.Tests.Support
{
    [TestClass]
    public class PartiallyBufferedStreamTests
    {
        [TestMethod]
        public void It_Reads_Synchronously()
        {
            // ARRANGE
            var buffer = Encoding.ASCII.GetBytes("foobarbaz");
            var innerStream = new MemoryStream(Encoding.ASCII.GetBytes("FOOBAR"));
            var stream = new PartiallyBufferedStream(buffer, 3, 3, innerStream);

            // ACT
            var content = new StreamReader(stream, Encoding.ASCII).ReadToEnd();

            // ASSERT
            content.Should().Be("barFOOBAR");
        }

        [TestMethod]
        public async Task It_Reads_Asynchronously()
        {
            // ARRANGE
            var buffer = Encoding.ASCII.GetBytes("foobarbaz");
            var innerStream = new MemoryStream(Encoding.ASCII.GetBytes("FOOBAR"));
            var stream = new PartiallyBufferedStream(buffer, 3, 3, innerStream);

            // ACT
            var content = await new StreamReader(stream, Encoding.ASCII).ReadToEndAsync();

            // ASSERT
            content.Should().Be("barFOOBAR");
        }
    }
}
