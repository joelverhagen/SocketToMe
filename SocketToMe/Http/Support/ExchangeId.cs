using System;

namespace Knapcode.SocketToMe.Http
{
    public struct ExchangeId
    {
        public static ExchangeId Empty => new ExchangeId(DateTimeOffset.MinValue, Guid.Empty);

        public ExchangeId(DateTimeOffset when, Guid unique)
        {
            When = when.ToUniversalTime();
            Unique = unique;
        }

        public DateTimeOffset When { get; }
        public Guid Unique { get; }

        public override string ToString()
        {
            return $"{When:O} {Unique:D}";
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

        public override int GetHashCode()
        {
            unchecked
            {
                return (When.GetHashCode()*397) ^ Unique.GetHashCode();
            }
        }
    }
}
