using System;
using System.Globalization;

namespace Knapcode.SocketToMe.Http
{
    public struct ExchangeId : IFormattable
    {
        public ExchangeId(DateTimeOffset when, Guid unique)
        {
            When = when.ToUniversalTime();
            Unique = unique;
        }

        public static ExchangeId Empty => new ExchangeId(DateTimeOffset.MinValue, Guid.Empty);

        public DateTimeOffset When { get; }
        public Guid Unique { get; }

        public static ExchangeId NewExchangeId()
        {
            return new ExchangeId(DateTimeOffset.UtcNow, Guid.NewGuid());
        }

        /// <summary>
        /// Converts the exchange ID to a string using the "O" format.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString("O");
        }

        /// <summary>
        /// Converts the exchange ID to a string based on the <see cref="format"/> parameter. The "D" format is a
        /// representation that is fixed width and lexicographically descending with respect to time. The "A" format is
        /// similar, but lexicographically ascending with respect to time. The "O" format is human readable format.
        /// Format strings are case insensitive. A null or empty format defaults to the "O" format.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <exception cref="FormatException">The provided <see cref="format"/> is not valid.</exception>
        /// <returns>The string representation of the <see cref="ExchangeId"/> value.</returns>
        public string ToString(string format)
        {
            switch (format)
            {
                case "d":
                case "D":
                    var descendingTicks = DateTimeOffset.MaxValue.Ticks - When.Ticks;
                    return string.Format(CultureInfo.InvariantCulture, "{0:D19}-{1:N}", descendingTicks, Unique);
                case "a":
                case "A":
                    var ascendingTicks = When.Ticks;
                    return string.Format(CultureInfo.InvariantCulture, "{0:D19}-{1:N}", ascendingTicks, Unique);
                case null:
                case "":
                case "o":
                case "O":
                    return string.Format(CultureInfo.InvariantCulture, "{0:O} {1:D}", When, Unique);
                default:
                    throw new FormatException($"The format '{format}' is not valid.");
            }
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString(format);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ExchangeId))
            {
                return false;
            }

            ExchangeId other = (ExchangeId) obj;
            return When.Equals(other.When) && Unique.Equals(other.Unique);
        }

        public static bool operator ==(ExchangeId a, ExchangeId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ExchangeId a, ExchangeId b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (When.GetHashCode()*397) ^ Unique.GetHashCode();
            }
        }
    }
}
