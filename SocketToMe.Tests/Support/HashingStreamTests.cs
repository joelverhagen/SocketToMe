using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Knapcode.SocketToMe.Support;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Support
{
    public class HashingStreamTests
    {
        [Theory, MemberData(nameof(GeneratesHashData))]
        public void GeneratesHashWhileReading(HashAlgorithm hashAlgorithm, string expectedContent, int bufferSize, string expectedHash)
        {
            // Arrange
            var inner = new MemoryStream(Encoding.ASCII.GetBytes(expectedContent));
            var hashing = new HashingStream(inner, hashAlgorithm, true);

            // Act
            string actualContent;
            using (var reader = new StreamReader(hashing, Encoding.ASCII, false, bufferSize))
            {
                actualContent = reader.ReadToEnd();
            }

            // Assert
            actualContent.Should().Be(expectedContent);
            var actualHash = GetHash(hashing.Hash);
            actualHash.Should().Be(expectedHash);
        }

        [Theory, MemberData(nameof(GeneratesHashData))]
        public void GeneratesHashWhileWriting(HashAlgorithm hashAlgorithm, string expectedContent, int bufferSize, string expectedHash)
        {
            // Arrange
            var inner = new MemoryStream();
            var hashing = new HashingStream(inner, hashAlgorithm, false);

            // Act
            using (var writer = new StreamWriter(hashing, Encoding.ASCII, bufferSize))
            {
                writer.Write(expectedContent);
            }

            // Assert
            var actualContent = Encoding.ASCII.GetString(inner.ToArray());
            actualContent.Should().Be(expectedContent);
            var actualHash = GetHash(hashing.Hash);
            actualHash.Should().Be(expectedHash);
        }

        [Fact]
        public void CannotWriteForReadMode()
        {
            // Arrange
            var hashing = new HashingStream(new MemoryStream(), SHA1.Create(), true);

            // Act, Assert
            Action action = () => hashing.Write(new byte[1], 0, 1);
            action.ShouldThrow<NotSupportedException>().Which.Message.Should().Be("The hashing stream is configured to read, not write.");
        }

        [Fact]
        public void CannotWriteForWriteMode()
        {
            // Arrange
            var hashing = new HashingStream(new MemoryStream(), SHA1.Create(), false);

            // Act, Assert
            Action action = () => hashing.Read(new byte[1], 0, 1);
            action.ShouldThrow<NotSupportedException>().Which.Message.Should().Be("The hashing stream is configured to write, not read.");
        }

        [Fact]
        public void HasNullHashBeforeClosingInWriteMode()
        {
            // Arrange
            var hashing = new HashingStream(new MemoryStream(), SHA1.Create(), false);

            // Act, Assert
            hashing.Hash.Should().BeNull();
        }

        [Fact]
        public void HasNullHashBeforeClosingAndFinishedReadingInReadMode()
        {
            // Arrange
            var hashing = new HashingStream(new MemoryStream(), SHA1.Create(), true);

            // Act, Assert
            hashing.Hash.Should().BeNull();
        }

        [Fact]
        public void HasHashAfterFinishedReadingInReadMode()
        {
            // Arrange
            var hashing = new HashingStream(new MemoryStream(new byte[0]), SHA1.Create(), true);

            // Act
            var read = hashing.Read(new byte[1], 0, 1);

            // Assert
            read.Should().Be(0);
            hashing.Hash.Should().NotBeNull();
            GetHash(hashing.Hash).Should().Be("DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        }

        [Fact]
        public void HashesPartialReadInReadMode()
        {
            // Arrange
            var inner = new MemoryStream(Encoding.ASCII.GetBytes("foobar"));
            var hashing = new HashingStream(inner, SHA1.Create(), true);
            var buffer = new byte[3];

            // Act
            var read = hashing.Read(buffer, 0, 3);
            hashing.Close();

            // Assert
            read.Should().Be(3);
            GetHash(hashing.Hash).Should().Be("0BEEC7B5EA3F0FDBC95D0DD47F3C5BC275DA8A33");
        }

        private string GetHash(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToUpper();
        }

        public static IEnumerable<object[]> GeneratesHashData
        {
            get
            {
                yield return new object[] {MD5.Create(), "foobar", 1, "3858F62230AC3C915F300C664312C63F"};
                yield return new object[] {MD5.Create(), "foobar", 4, "3858F62230AC3C915F300C664312C63F"};
                yield return new object[] {MD5.Create(), "foobar", 100, "3858F62230AC3C915F300C664312C63F"};
                yield return new object[] {SHA1.Create(), "foobar", 1, "8843D7F92416211DE9EBB963FF4CE28125932878"};
                yield return new object[] {SHA1.Create(), "foobar", 4, "8843D7F92416211DE9EBB963FF4CE28125932878"};
                yield return new object[] {SHA1.Create(), "foobar", 100, "8843D7F92416211DE9EBB963FF4CE28125932878"};
            }
        }
    }
}
