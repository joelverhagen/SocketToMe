using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Http;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Http
{
    public class ChunkedStreamTests
    {
        [Fact]
        public void It_Reads_Multiple_Chunks_With_A_Small_Buffer_Synchronously()
        {
            // ARRANGE
            var ts = new TestState { BufferSize = 1 };

            // ACT, ASSERT
            ts.ReadAndVerify(ts.MultipleChunks);
        }

        [Fact]
        public void It_Reads_Multiple_Chunks_With_A_Large_Buffer_Synchronously()
        {
            // ARRANGE
            var ts = new TestState { BufferSize = 1024 };

            // ACT, ASSERT
            ts.ReadAndVerify(ts.MultipleChunks);
        }

        [Fact]
        public void It_Reads_One_Chunk_Synchronously()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT, ASSERT
            ts.ReadAndVerify(ts.MultipleChunks);
        }

        [Fact]
        public async Task It_Reads_Multiple_Chunks_With_A_Small_Buffer_Asynchronously()
        {
            // ARRANGE
            var ts = new TestState { BufferSize = 1 };

            // ACT, ASSERT
            await ts.ReadAndVerifyAsync(ts.MultipleChunks);
        }

        [Fact]
        public async Task It_Reads_Multiple_Chunks_With_A_Large_Buffer_Asynchronously()
        {
            // ARRANGE
            var ts = new TestState { BufferSize = 1024 };

            // ACT, ASSERT
            await ts.ReadAndVerifyAsync(ts.MultipleChunks);
        }

        [Fact]
        public async Task It_Reads_One_Chunk_Asynchronously()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT, ASSERT
            await ts.ReadAndVerifyAsync(ts.MultipleChunks);
        }

        private class TestState
        {
            public TestState()
            {
                // data
                MultipleChunks = new[] { "Wiki", "pedia", " in\r\n\r\nchunks." };
                OneChunk = new[] { "\r\n" };
                BufferSize = 1;
            }

            public string[] JustCrLf { get; set; }

            public string[] OneChunk { get; set; }

            public int BufferSize { get; set; }

            public string[] MultipleChunks { get; set; }

            public ChunkedStream Target { get; set; }

            public void Setup(string[] chunks)
            {
                Target = new ChunkedStream(GetStream(chunks));
            }

            public async Task ReadAndVerifyAsync(string[] chunks)
            {
                // ARRANGE
                Setup(chunks);
                var output = new MemoryStream();

                // ACT
                await Target.CopyToAsync(output, BufferSize);
                var content = Encoding.ASCII.GetString(output.ToArray());

                // ASSERT
                content.Should().Be(string.Join(string.Empty, chunks));
            }

            public void ReadAndVerify(string[] chunks)
            {
                // ARRANGE
                Setup(chunks);
                var output = new MemoryStream();

                // ACT
                Target.CopyTo(output, BufferSize);
                var content = Encoding.ASCII.GetString(output.ToArray());

                // ASSERT
                content.Should().Be(string.Join(string.Empty, chunks));
            }

            private Stream GetStream(IEnumerable<string> chunks)
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream, Encoding.ASCII, 4096, true);
                foreach (var chunk in chunks)
                {
                    writer.Write(chunk.Length.ToString("X"));
                    writer.Write("\r\n");
                    writer.Write(chunk);
                    writer.Write("\r\n");
                }

                writer.Write("0");
                writer.Write("\r\n\r\n");
                writer.Flush();

                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        }
    }
}
