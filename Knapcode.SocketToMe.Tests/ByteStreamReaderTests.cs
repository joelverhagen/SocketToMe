using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knapcode.SocketToMe.Tests
{
    [TestClass]
    public class ByteStreamReaderTests
    {
        [TestMethod]
        public async Task It_Returns_Null_After_Reading_The_Last_Line()
        {
            // ARRANGE
            var ts = new TestState();
            ts.Setup(ts.OneLine);

            // ACT
            string line1 = await ts.Reader.ReadLineAsync();
            string line2 = await ts.Reader.ReadLineAsync();
            string line3 = await ts.Reader.ReadLineAsync();

            // ASSERT
            line1.Should().Be(ts.OneLine.First());
            line2.Should().BeNull();
            line3.Should().BeNull();
        }

        [TestMethod]
        public async Task It_Can_Trim_Line_Endings()
        {
            // ARRANGE
            var ts = new TestState {PreserveLineEndings = false};
            ts.Setup(ts.OneLine);

            // ACT
            string line = await ts.Reader.ReadLineAsync();

            // ASSERT
            ts.OneLine.First().Trim().Should().Be(line);
        }

        [TestMethod]
        public async Task It_Returns_All_Contents_With_A_Small_Buffer()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT, ASSERT
            await ts.VerifyReadLineThenReadBytesAsync(ts.TrailingLineEndings, 1);
        }

        [TestMethod]
        public async Task It_Returns_All_Contents_With_A_Large_Buffer()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT, ASSERT
            await ts.VerifyReadLineThenReadBytesAsync(ts.TrailingLineEndings, ts.GetLength(ts.TrailingLineEndings) * 2);
        }

        [TestMethod]
        public async Task It_Returns_Correct_Line_With_Large_Buffer()
        {
            // ARRANGE
            var ts = new TestState();
            ts.BufferSize = ts.GetLength(ts.TwoLines) * 2;

            // ACT, ASSERT
            await ts.VerifyAllLinesAsync(ts.TwoLines);
        }

        [TestMethod]
        public async Task It_Can_Read_All_Lines_With_Trailing_Line_Endings()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT, ASSERT
            await ts.VerifyAllLinesAsync(ts.TrailingLineEndings);
        }

        [TestMethod]
        public async Task It_Can_Read_All_Lines_With_No_Trailing_Line_Endings()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT, ASSERT
            await ts.VerifyAllLinesAsync(ts.NoTrailingLineEndings);
        }

        [TestMethod]
        public async Task It_Can_Read_All_Lines_With_An_Empty_Line()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT, ASSERT
            await ts.VerifyAllLinesAsync(ts.EmptyLine);
        }

        [TestMethod]
        public async Task It_Can_Read_All_Lines_While_Ignoring_Non_Cr_Lf_Line_Endings()
        {
            // ARRANGE
            var ts = new TestState();

            // ACT, ASSERT
            await ts.VerifyAllLinesAsync(ts.NonCrLfLineEndings);
        }

        private class TestState
        {
            public TestState()
            {
                // data
                OneLine = new[] { "First\r\n" };
                TwoLines = new[] { "First\r\n", "Second\r\n" };
                TrailingLineEndings = new[] {"First\r\n", "Second\r\n", "Third\r\n"};
                NoTrailingLineEndings = new[] { "First\r\n", "Second\r\n", "Third" };
                EmptyLine = new[] { "First\r\n", "\r\n", "Third\r\n" };
                NonCrLfLineEndings = new[] { "This\ris\nthe first line\r\n", "Second\r\n" };

                // dependencies
                Stream = null;
                BufferSize = 1;
                PreserveLineEndings = true;

                // unit
                Reader = null;
            }

            public bool PreserveLineEndings { get; set; }

            public string[] TwoLines { get; set; }

            public string[] OneLine { get; set; }

            public string[] EmptyLine { get; set; }

            public string[] NoTrailingLineEndings { get; set; }

            public string[] TrailingLineEndings { get; set; }

            public string[] NonCrLfLineEndings { get; set; }

            public Stream Stream { get; set; }

            public int BufferSize { get; set; }

            public ByteStreamReader Reader { get; set; }

            public Stream GetStream(string[] lines)
            {
                var bytes = Encoding.GetBytes(string.Join(string.Empty, lines));
                var stream = new MemoryStream(bytes);
                return stream;
            }

            public Encoding Encoding
            {
                get { return new UTF8Encoding(false); }
            }

            public int GetLength(string[] lines)
            {
                return GetString(lines).Length;
            }

            public void Setup(string[] lines)
            {
                Stream = GetStream(lines);
                Reader = new ByteStreamReader(Stream, BufferSize, PreserveLineEndings); 
            }

            public async Task<string> ReadToEndAsync(string[] expectedLines)
            {
                var buffer = new byte[GetLength(expectedLines)];
                var outputStream = new MemoryStream();
                int read = -1;
                while (read != 0)
                {
                    read = await Reader.ReadAsync(buffer, 0, buffer.Length);
                    outputStream.Write(buffer, 0, read);
                }

                return Encoding.GetString(outputStream.ToArray());
            }

            public string GetString(string[] lines)
            {
                return string.Join(string.Empty, lines);
            }

            public async Task VerifyReadLineThenReadBytesAsync(string[] expectedLines, int bufferSize)
            {
                // ARRANGE
                BufferSize = bufferSize;
                Setup(expectedLines);

                // ACT
                string line = await Reader.ReadLineAsync();
                string rest = await ReadToEndAsync(expectedLines);

                // ASSERT
                GetString(expectedLines).Should().Be(line + rest);
            }

            public async Task VerifyAllLinesAsync(string[] expectedLines)
            {
                // ARRANGE
                Setup(expectedLines);

                // ACT
                var actualLines = new List<string>();
                string line;
                while ((line = await Reader.ReadLineAsync()) != null)
                {
                    actualLines.Add(line);
                }

                // ASSERT
                actualLines.Should().HaveCount(expectedLines.Length);
                for (int i = 0; i < expectedLines.Length; i++)
                {
                    actualLines[i].Should().Be(expectedLines[i]);
                }
            }
        }
    }
}
