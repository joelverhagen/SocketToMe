using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Knapcode.SocketToMe.Http;
using Knapcode.SocketToMe.Tests.TestSupport;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Http
{
    public class GzipStoreTests
    {
        private const string Key = nameof(Key);
        private const string InputText = "This is the long input text. This is the long input text. Repeated.";
        private static readonly byte[] OptimalCompressInputText =
        {
            31, 139, 8, 0, 0, 0, 0, 0, 4, 0, 11, 201, 200, 44, 86, 0, 162, 146, 140, 84, 133, 156, 252, 188, 116,
            133, 204, 188, 130, 210, 18, 133, 146, 212, 138, 18, 61, 133, 16, 124, 146, 65, 169, 5, 169, 137, 37,
            169, 41, 122, 0, 49, 213, 255, 81, 67, 0, 0, 0
        };

        [Fact]
        public async Task SupportsRoundTrip()
        {
            // Arrange
            var memoryStore = new InMemoryStore();
            var gzipStore = new GzipStore(memoryStore, CompressionLevel.Optimal);

            // Act, Assert
            await gzipStore.SetAsync(Key, GetStream(InputText), CancellationToken.None);
            VerifyStringAsync(gzipStore, Key, InputText);
        }

        [Fact]
        public async Task DecompressesFromExpectedBytes()
        {
            // Arrange
            var memoryStore = new InMemoryStore();
            await memoryStore.SetAsync(Key, new MemoryStream(OptimalCompressInputText), CancellationToken.None);
            var gzipStore = new GzipStore(memoryStore, CompressionLevel.Optimal);

            // Act, Assert
            await VerifyStringAsync(gzipStore, Key, InputText);
        }

        [Fact]
        public async Task CompressesToExpectedBytes()
        {
            // Arrange
            var memoryStore = new InMemoryStore();
            var gzipStore = new GzipStore(memoryStore, CompressionLevel.Optimal);

            // Act
            await gzipStore.SetAsync(Key, GetStream(InputText), CancellationToken.None);

            // Assert
            await VerifyBytesAsync(memoryStore, Key, OptimalCompressInputText);
        }

        [Theory, MemberData(nameof(AdheresToProvidedCompressionLevelData))]
        public async Task AdheresToProvidedCompressionLevel(CompressionLevel compression)
        {
            // Arrange
            var expected = Compress(InputText, compression);
            var memoryStore = new InMemoryStore();
            var gzipStore = new GzipStore(memoryStore, compression);

            // Act
            await gzipStore.SetAsync(Key, GetStream(InputText), CancellationToken.None);

            // Assert
            await VerifyBytesAsync(memoryStore, Key, expected);
        }

        public static IEnumerable<object[]> AdheresToProvidedCompressionLevelData
        {
            get
            {
                return Enum
                    .GetValues(typeof (CompressionLevel))
                    .OfType<CompressionLevel>()
                    .Select(c => new object[] {c});
            }
        }

        private byte[] Compress(string input, CompressionLevel compression)
        {
            var destination = new MemoryStream();
            using (var compressStream = new GZipStream(destination, compression))
            {
                GetStream(input).CopyTo(compressStream);
            }

            return destination.ToArray();
        }

        private byte[] GetBytes(Stream stream)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        private string GetString(Stream stream)
        {
            var bytes = GetBytes(stream);
            return Encoding.ASCII.GetString(bytes);
        }

        private MemoryStream GetStream(string input)
        {
            var bytes = Encoding.ASCII.GetBytes(input);
            return new MemoryStream(bytes);
        }

        private async Task VerifyBytesAsync(IStore store, string key, byte[] expected)
        {
            var stream = await store.GetAsync(key, CancellationToken.None);
            var actual = GetBytes(stream);
            actual.ShouldBeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        private async Task VerifyStringAsync(IStore store, string key, string expected)
        {
            var stream = await store.GetAsync(key, CancellationToken.None);
            var actual = GetString(stream);
            actual.Should().Be(expected);
        }
    }
}
