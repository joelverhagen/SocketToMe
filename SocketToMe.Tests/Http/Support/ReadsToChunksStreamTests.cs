using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Http;
using Knapcode.SocketToMe.Http.Support;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Http.Support
{
    public class ReadsToChunksStreamTests
    {
        [Fact]
        public async Task RejectsTooSmallBuffer()
        {
            // Arrange
            var chunks = new ReadsToChunksStream(new MemoryStream());

            // Act, Assert
            Func<Task> actionAsync = () => chunks.ReadAsync(new byte[5], 0, 5);
            actionAsync.ShouldThrow<ArgumentOutOfRangeException>().Which.Message.Should().StartWith("The number of bytes to read must be greater than or equal to 6.");
        }

        [Fact]
        public async Task ReadsEmptyBuffer()
        {
            // Arrange, Act
            var chunks = await GetReadBuffersAsync(string.Empty, 20);

            // Act
            chunks.ShouldBeEquivalentTo(
                new[]
                {
                    "0\r\n\r\n"
                },
                o => o.WithStrictOrdering());
        }

        [Fact]
        public async Task OneChunk()
        {
            // Arrange, Act
            var chunks = await GetReadBuffersAsync("0123456789AB", 20);

            // Act
            chunks.ShouldBeEquivalentTo(
                new[]
                {
                    "c\r\n0123456789AB\r\n",
                    "0\r\n\r\n"
                },
                o => o.WithStrictOrdering());
        }

        [Fact]
        public async Task FullChunks()
        {
            // Arrange, Act
            var chunks = await GetReadBuffersAsync("0123456789AB", 10);

            // Assert
            chunks.ShouldBeEquivalentTo(
                new[]
                {
                    "5\r\n01234\r\n",
                    "5\r\n56789\r\n",
                    "2\r\nAB\r\n",
                    "0\r\n\r\n"
                },
                o => o.WithStrictOrdering());
        }

        [Fact]
        public async Task Shift()
        {
            // Arrange, Act
            var chunks = await GetReadBuffersAsync("012345", 500);

            // Assert
            chunks.ShouldBeEquivalentTo(
                new[]
                {
                    "6\r\n012345\r\n",
                    "0\r\n\r\n"
                },
                o => o.WithStrictOrdering());
        }

        [Fact]
        public async Task ReadOne()
        {
            // Arrange, Act
            var chunks = await GetReadBuffersAsync("012", 6);

            // Assert
            chunks.ShouldBeEquivalentTo(
                new[]
                {
                    "1\r\n0\r\n",
                    "1\r\n1\r\n",
                    "1\r\n2\r\n",
                    "0\r\n\r\n"
                },
                o => o.WithStrictOrdering());
        }

        [Fact]
        public async Task RoundTrip()
        {
            // Arrange
            var expected = "This is the content the will go through both chunk streams.";
            var inner = new MemoryStream(Encoding.ASCII.GetBytes(expected));
            var readToChunks = new ReadsToChunksStream(inner);
            var readFromChunks = new ReadsFromChunksStream(readToChunks);
            var outputStream = new MemoryStream();

            // Act
            await readFromChunks.CopyToAsync(outputStream, 10);

            // Assert
            Encoding.ASCII.GetString(outputStream.ToArray()).Should().Be(expected);
        }

        private static async Task<IEnumerable<string>> GetReadBuffersAsync(string input, int bufferSize)
        {
            // Arrange
            var stream = new ReadsToChunksStream(new MemoryStream(Encoding.ASCII.GetBytes(input)));
            var buffers = new List<string>();

            int read = -1;
            while (read != 0)
            {
                var buffer = new byte[bufferSize];

                // Act
                read = await stream.ReadAsync(buffer, 0, bufferSize);

                if (read > 0)
                {
                    buffers.Add(Encoding.ASCII.GetString(buffer, 0, read));
                }
            }

            return buffers;
        }
    }
}
