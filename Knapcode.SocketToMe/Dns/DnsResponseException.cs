using System;
using System.Runtime.Serialization;

namespace Knapcode.SocketToMe.Dns
{
    [Serializable]
    public class DnsResponseException : Exception
    {
        protected DnsResponseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public DnsResponseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DnsResponseException(string message) : base(message)
        {
        }
    }
}