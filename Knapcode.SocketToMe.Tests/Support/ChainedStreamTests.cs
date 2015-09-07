using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Support;
using Knapcode.SocketToMe.Tests.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Knapcode.SocketToMe.Tests.Support
{
    [TestClass]
    public class ChainedStreamTests
    {
        private static readonly byte[] BytesA = Encoding.ASCII.GetBytes("ABCD");
        private static readonly byte[] BytesB = Encoding.ASCII.GetBytes("1234");
        [TestMethod, TestCategory("Unit")]
        public void DisposeWithNoStreams()
        {
            // ARRANGE
            var chain = new ChainedStream(Enumerable.Empty<Stream>());
            Action action = () => chain.Dispose();

            // ACT, ASSERT
            action.ShouldNotThrow();
        }

        [TestMethod, TestCategory("Unit")]
        public void DisposeWhenNotStarted()
        {
            // ARRANGE
            var streamA = new DisposedStream(8);
            var streamB = new DisposedStream(8);
            var chain = new ChainedStream(new[] { streamA, streamB });

            // ACT
            chain.Dispose();

            // ASSERT
            streamA.Disposed.Should().BeTrue();
            streamB.Disposed.Should().BeTrue();
        }

        [TestMethod, TestCategory("Unit")]
        public void DisposeWhenOneStarted()
        {
            // ARRANGE
            var streamA = new DisposedStream(8);
            var streamB = new DisposedStream(8);
            var chain = new ChainedStream(new[] { streamA, streamB });
            chain.Read(new byte[4], 0, 4);

            // ACT
            chain.Dispose();

            // ASSERT
            streamA.Disposed.Should().BeTrue();
            streamB.Disposed.Should().BeTrue();
        }

        [TestMethod, TestCategory("Unit")]
        public void DisposeWhenOneFinished()
        {
            // ARRANGE
            var streamA = new DisposedStream(8);
            var streamB = new DisposedStream(8);
            var chain = new ChainedStream(new[] { streamA, streamB });
            chain.Read(new byte[12], 0, 12);

            // ACT
            chain.Dispose();

            // ASSERT
            streamA.Disposed.Should().BeTrue();
            streamB.Disposed.Should().BeTrue();
        }

        [TestMethod, TestCategory("Unit")]
        public void DisposeWhenAllFinished()
        {
            // ARRANGE
            var streamA = new DisposedStream(8);
            var streamB = new DisposedStream(8);
            var chain = new ChainedStream(new[] { streamA, streamB });
            chain.Read(new byte[16], 0, 16);

            // ACT
            chain.Dispose();

            // ASSERT
            streamA.Disposed.Should().BeTrue();
            streamB.Disposed.Should().BeTrue();
        }

        [TestMethod, TestCategory("Unit")]
        public void Read_WithNoStreams_ReturnsZero()
        {
            Read_WithNoStreams_ReturnsZero((s, b, o, c) => s.Read(b, o, c));
        }

        [TestMethod, TestCategory("Unit")]
        public void Read_WhenDesiredIsGreaterThanAvailable_ReadsProperly()
        {
            Read_WhenDesiredIsGreaterThanAvailable_ReadsProperly((s, b, o, c) => s.Read(b, o, c));
        }

        [TestMethod, TestCategory("Unit")]
        public void Read_WhenAvailableIsGreaterThanDesired_ReadsProperly()
        {
            Read_WhenAvailableIsGreaterThanDesired_ReadsProperly((s, b, o, c) => s.Read(b, o, c));
        }

        [TestMethod, TestCategory("Unit")]
        public void Read_WhenAvailableIsSameAsDesired_ReadsProperly()
        {
            Read_WhenAvailableIsSameAsDesired_ReadsProperly((s, b, o, c) => s.Read(b, o, c));
        }

        [TestMethod, TestCategory("Unit")]
        public void Read_WithMultipleReads_ConsumesEverything()
        {
            Read_WithMultipleReads_ConsumesEverything((s, b, o, c) => s.Read(b, o, c));
        }

        [TestMethod, TestCategory("Unit")]
        public void Read_WhenStreamsReturnVariableCount_ReadsProperly()
        {
            Read_WhenStreamsReturnVariableCount_ReadsProperly((s, b, o, c) => s.Read(b, o, c));
        }

        [TestMethod, TestCategory("Unit")]
        public void Read_WhenDisposeIsEnabled_DisposesFinishedStreams()
        {
            Read_WhenDisposeIsEnabled_DisposesFinishedStreams((s, b, o, c) => s.Read(b, o, c));
        }

        [TestMethod, TestCategory("Unit")]
        public void Read_WhenDisposeIsDisabled_DoesNotDisposeStreams()
        {
            Read_WhenDisposeIsDisabled_DoesNotDisposeStreams((s, b, o, c) => s.Read(b, o, c));
        }

        [TestMethod, TestCategory("Unit")]
        public void ReadAsync_WithNoStreams_ReturnsZero()
        {
            Read_WithNoStreams_ReturnsZero((s, b, o, c) => s.ReadAsync(b, o, c).Result);
        }

        [TestMethod, TestCategory("Unit")]
        public void ReadAsync_WhenDesiredIsGreaterThanAvailable_ReadsProperly()
        {
            Read_WhenDesiredIsGreaterThanAvailable_ReadsProperly((s, b, o, c) => s.ReadAsync(b, o, c).Result);
        }

        [TestMethod, TestCategory("Unit")]
        public void ReadAsync_WhenAvailableIsGreaterThanDesired_ReadsProperly()
        {
            Read_WhenAvailableIsGreaterThanDesired_ReadsProperly((s, b, o, c) => s.ReadAsync(b, o, c).Result);
        }

        [TestMethod, TestCategory("Unit")]
        public void ReadAsync_WhenAvailableIsSameAsDesired_ReadsProperly()
        {
            Read_WhenAvailableIsSameAsDesired_ReadsProperly((s, b, o, c) => s.ReadAsync(b, o, c).Result);
        }

        [TestMethod, TestCategory("Unit")]
        public void ReadAsync_WithMultipleReads_ConsumesEverything()
        {
            Read_WithMultipleReads_ConsumesEverything((s, b, o, c) => s.ReadAsync(b, o, c).Result);
        }

        [TestMethod, TestCategory("Unit")]
        public void ReadAsync_WhenStreamsReturnVariableCount_ReadsProperly()
        {
            Read_WhenStreamsReturnVariableCount_ReadsProperly((s, b, o, c) => s.ReadAsync(b, o, c).Result);
        }

        [TestMethod, TestCategory("Unit")]
        public void ReadAsync_WhenDisposeIsEnabled_DisposesFinishedStreams()
        {
            Read_WhenDisposeIsEnabled_DisposesFinishedStreams((s, b, o, c) => s.ReadAsync(b, o, c).Result);
        }

        [TestMethod, TestCategory("Unit")]
        public void ReadAsync_WhenDisposeIsDisabled_DoesNotDisposeStreams()
        {
            Read_WhenDisposeIsDisabled_DoesNotDisposeStreams((s, b, o, c) => s.ReadAsync(b, o, c).Result);
        }

        private static void Read_WithNoStreams_ReturnsZero(Func<ChainedStream, byte[], int, int, int> readFunc)
        {
            // ARRANGE
            var chain = new ChainedStream(Enumerable.Empty<Stream>());

            // ACT
            var read = readFunc(chain, new byte[12], 0, 12);

            // ASSERT
            read.Should().Be(0);
        }

        private static void Read_WhenDesiredIsGreaterThanAvailable_ReadsProperly(Func<ChainedStream, byte[], int, int, int> readFunc)
        {
            // ARRANGE
            const int availableBytes = 8;
            const int desiredBytes = 10;
            var buffer = new byte[12];
            var chain = new ChainedStream(new[] {new MemoryStream(BytesA), new MemoryStream(BytesB)});

            // ACT
            var read = readFunc(chain, buffer, 2, desiredBytes);

            // ASSERT
            read.Should().Be(availableBytes);
            buffer.ShouldBeEquivalentTo(new byte[2].Concat(BytesA).Concat(BytesB).Concat(new byte[2]));
        }

        private static void Read_WhenAvailableIsGreaterThanDesired_ReadsProperly(Func<ChainedStream, byte[], int, int, int> readFunc)
        {
            // ARRANGE
            const int desiredBytes = 6;
            var buffer = new byte[desiredBytes + 4];
            var chain = new ChainedStream(new[] {new MemoryStream(BytesA), new MemoryStream(BytesB)});

            // ACT
            var read = readFunc(chain, buffer, 2, desiredBytes);

            // ASSERT
            read.Should().Be(desiredBytes);
            buffer.ShouldBeEquivalentTo(new byte[2].Concat(BytesA).Concat(BytesB.Take(2)).Concat(new byte[2]));
        }

        private static void Read_WhenAvailableIsSameAsDesired_ReadsProperly(Func<ChainedStream, byte[], int, int, int> readFunc)
        {
            // ARRANGE
            const int availableBytes = 8;
            var buffer = new byte[availableBytes + 4];
            var chain = new ChainedStream(new[] {new MemoryStream(BytesA), new MemoryStream(BytesB)});

            // ACT
            var read = readFunc(chain, buffer, 2, availableBytes);

            // ASSERT
            read.Should().Be(availableBytes);
            buffer.ShouldBeEquivalentTo(new byte[2].Concat(BytesA).Concat(BytesB).Concat(new byte[2]));
        }

        private static void Read_WithMultipleReads_ConsumesEverything(Func<ChainedStream, byte[], int, int, int> readFunc)
        {
            // ARRANGE
            var buffer = new byte[12];
            var chain = new ChainedStream(new[] {new MemoryStream(BytesA), new MemoryStream(BytesB)});

            // ACT
            var readA = readFunc(chain, buffer, 2, 6);
            var readB = readFunc(chain, buffer, 8, 4);

            // ASSERT
            readA.Should().Be(6);
            readB.Should().Be(2);
            buffer.ShouldBeEquivalentTo(new byte[2].Concat(BytesA).Concat(BytesB).Concat(new byte[2]));
        }

        private static void Read_WhenStreamsReturnVariableCount_ReadsProperly(Func<ChainedStream, byte[], int, int, int> readFunc)
        {
            // ARRANGE
            const int availableBytes = 16;
            const int desiredBytes = 20;
            var buffer = new byte[desiredBytes];
            var chain = new ChainedStream(new[] {GetVariableReadStreamMock(8, 97).Object, GetVariableReadStreamMock(8, 98).Object});

            // ACT
            var read = readFunc(chain, buffer, 2, desiredBytes);

            // ASSERT
            read.Should().Be(availableBytes);
            buffer.ShouldBeEquivalentTo(Enumerable.Empty<byte>()
                .Concat(new byte[2])
                .Concat(Enumerable.Repeat((byte) 97, 8))
                .Concat(Enumerable.Repeat((byte) 98, 8))
                .Concat(new byte[2]));
        }

        private static void Read_WhenDisposeIsEnabled_DisposesFinishedStreams(Func<ChainedStream, byte[], int, int, int> readFunc)
        {
            // ARRANGE
            var streamA = new DisposedStream(8);
            var streamB = new DisposedStream(8);
            var chain = new ChainedStream(new[] {streamA, streamB});

            // ACT
            readFunc(chain, new byte[12], 0, 12);

            // ASSERT
            streamA.Disposed.Should().BeTrue();
            streamB.Disposed.Should().BeFalse();
        }

        private static void Read_WhenDisposeIsDisabled_DoesNotDisposeStreams(Func<ChainedStream, byte[], int, int, int> readFunc)
        {
            // ARRANGE
            var streamA = new DisposedStream(8);
            var streamB = new DisposedStream(8);
            var chain = new ChainedStream(new[] {streamA, streamB}, false);

            // ACT
            readFunc(chain, new byte[12], 0, 12);

            // ASSERT
            streamA.Disposed.Should().BeFalse();
            streamB.Disposed.Should().BeFalse();
        }

        private static Mock<Stream> GetVariableReadStreamMock(int size, byte b)
        {
            var remaining = size;

            var random = new Random();
            var mock = new Mock<Stream>();

            mock
                .Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((buffer, offset, count) =>
                {
                    if (remaining == 0)
                    {
                        return 0;
                    }

                    count = Math.Min(remaining, count);

                    var read = random.Next(1, count + 1);
                    for (var i = 0; i < read; i++)
                    {
                        buffer[offset + i] = b;
                    }

                    remaining -= read;

                    return read;
                });

            mock
                .Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns<byte[], int, int, CancellationToken>((buffer, offset, count, cancellationToken) => Task.FromResult(mock.Object.Read(buffer, offset, count)));

            return mock;
        }
    }
}