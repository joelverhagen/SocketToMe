using System;

namespace Knapcode.SocketToMe.Sandbox.Extensions
{
    public static class ByteExtensions
    {
        /// <summary>
        /// Set the bit at the provided index to the provided value.
        /// </summary>
        /// <param name="b">The byte.</param>
        /// <param name="index">The bit index, starting at 0, where 0 indicates the least significate bit.</param>
        /// <param name="value">The bit value.</param>
        /// <returns>The new byte.</returns>
        public static byte SetBit(this byte b, int index, bool value)
        {
            if (index < 0 || index > 7)
            {
                throw new ArgumentOutOfRangeException("index", "The bit index must be between 0 and 7 (inclusive).");
            }

            int mask = 1 << index;
            return (byte)(value ? b | mask : b & ~mask);
        }

        /// <summary>
        /// Get the bit value at the provided index.
        /// </summary>
        /// <param name="b">The byte.</param>
        /// <param name="index">The bit index, starting at 0, where 0 indicates the least significate bit.</param>
        /// <returns>The bit value.</returns>
        public static bool GetBit(this byte b, int index)
        {
            if (index < 0 || index > 7)
            {
                throw new ArgumentOutOfRangeException("index", "The bit index must be between 0 and 7 (inclusive).");
            }

            return (b & (1 << index)) != 0;
        }
    }
}
