using System;

namespace Knapcode.SocketToMe.Dns.Exceptions
{
    [Serializable]
    public class InvalidDnsResponseException : Exception
    {
        public InvalidDnsResponseException(string message, byte[] content, Exception innerException)
            : base(message, innerException)
        {
            Content = content;
        }

        public InvalidDnsResponseException(string message, byte[] content)
            : base(message)
        {
            Content = content;
        }

        public byte[] Content { get; private set; }
    }
}