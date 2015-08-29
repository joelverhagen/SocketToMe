using System;
using Knapcode.SocketToMe.Sandbox.Extensions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knapcode.SocketToMe.Tests.Extensions
{
    [TestClass]
    public class ByteExtensionsTests
    {
        [TestMethod]
        public void GetBit_WithFalseBit_ReturnsFalse()
        {
            // ARRANGE
            const byte b = 0;

            // ACT
            bool bit = b.GetBit(0);

            // ASSERT
            bit.Should().BeFalse();
        }

        [TestMethod]
        public void GetBit_WithTrueBit_ReturnsTrue()
        {
            // ARRANGE
            const byte b = 1;

            // ACT
            bool bit = b.GetBit(0);

            // ASSERT
            bit.Should().BeTrue();
        }

        [TestMethod]
        public void SetBit_WithTrueBit_SetsTrue()
        {
            // ARRANGE
            byte b = 0;

            // ACT
            b = b.SetBit(0, true);

            // ASSERT
            b.GetBit(0).Should().BeTrue();
        }

        [TestMethod]
        public void SetBit_WithFalseBit_SetsFalse()
        {
            // ARRANGE
            byte b = 1;

            // ACT
            b = b.SetBit(0, false);

            // ASSERT
            b.GetBit(0).Should().BeFalse();
        }

        [TestMethod]
        public void SetBit_WithNegativeIndex_ThrowsException()
        {
            // ACT
            Action a = () => byte.MinValue.SetBit(-1, true);

            // ASSERT
            a.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("index");
        }

        [TestMethod]
        public void SetBit_WithTooLargeIndex_ThrowsException()
        {
            // ACT
            Action a = () => byte.MinValue.SetBit(8, true);

            // ASSERT
            a.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("index");
        }

        [TestMethod]
        public void GetBit_WithNegativeIndex_ThrowsException()
        {
            // ACT
            Action a = () => byte.MinValue.GetBit(-1);

            // ASSERT
            a.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("index");
        }

        [TestMethod]
        public void GetBit_WithTooLargeIndex_ThrowsException()
        {
            // ACT
            Action a = () => byte.MinValue.GetBit(8);

            // ASSERT
            a.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("index");
        }
    }
}
