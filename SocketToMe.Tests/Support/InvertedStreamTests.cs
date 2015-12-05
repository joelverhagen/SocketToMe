using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Knapcode.SocketToMe.Support;
using Knapcode.SocketToMe.Tests.TestSupport;
using Xunit;

namespace Knapcode.SocketToMe.Tests.Support
{
    public class InvertedStreamTests
    {
        public enum BufferSize
        {
            One,
            Small,
            Medium,
            Large
        }

        public enum Mode
        {
            Reduce,
            Expand,
            Replace,
            Identity
        }

        private static readonly byte UnderscoreByte = Encoding.ASCII.GetBytes("_")[0];

        [Theory, MemberData(nameof(InvertsStreamData))]
        public void InvertsStream(Mode mode, int bufferSize, string expected)
        {
            // Arrange
            Func<byte[], IEnumerable<byte>> map;
            switch (mode)
            {
                case Mode.Reduce:
                    map = bytes => bytes.Where((b, i) => i%4 == 0);
                    break;
                case Mode.Expand:
                    map = bytes => bytes.Concat(new[] {UnderscoreByte});
                    break;
                case Mode.Replace:
                    map = bytes => bytes.Select(b => UnderscoreByte);
                    break;
                case Mode.Identity:
                    map = bytes => bytes;
                    break;
                default:
                    throw new NotSupportedException($"The mode '{mode}' is not supported.");
            }

            var source = new MemoryStream(Encoding.ASCII.GetBytes("These are the source bytes!"));
            var inverted = new InvertedStream(source, buffer => new MapStream(buffer, map));
            var destination = new MemoryStream();

            // Act
            using (inverted)
            {
                inverted.CopyTo(destination, bufferSize);
            }

            // Assert
            var actual = Encoding.ASCII.GetString(destination.ToArray());
            actual.Should().Be(expected);
        }

        public static IEnumerable<object[]> InvertsStreamData
        {
            get
            {
                yield return new object[] { Mode.Reduce, 1, "These are the source bytes!" };
                yield return new object[] { Mode.Reduce, 4, "Teeeu e" };
                yield return new object[] { Mode.Reduce, 12, "Teeeu e" };
                yield return new object[] { Mode.Reduce, 1000, "Teeeu e" };
                yield return new object[] { Mode.Expand, 1, "T_h_e_s_e_ _a_r_e_ _t_h_e_ _s_o_u_r_c_e_ _b_y_t_e_s_!_" };
                yield return new object[] { Mode.Expand, 4, "Thes_e ar_e th_e so_urce_ byt_es!_" };
                yield return new object[] { Mode.Expand, 12, "These are th_e source byt_es!_" };
                yield return new object[] { Mode.Expand, 1000, "These are the source bytes!_" };
                yield return new object[] { Mode.Replace, 1, "___________________________" };
                yield return new object[] { Mode.Replace, 4, "___________________________" };
                yield return new object[] { Mode.Replace, 12, "___________________________" };
                yield return new object[] { Mode.Replace, 1000, "___________________________" };
                yield return new object[] { Mode.Identity, 1, "These are the source bytes!" };
                yield return new object[] { Mode.Identity, 4, "These are the source bytes!" };
                yield return new object[] { Mode.Identity, 12, "These are the source bytes!" };
                yield return new object[] { Mode.Identity, 1000, "These are the source bytes!" };
            }
        }
    }
}