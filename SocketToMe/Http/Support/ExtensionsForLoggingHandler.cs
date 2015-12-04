using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Knapcode.SocketToMe.Http
{
    public static class ExtensionsForLoggingHandler
    {
        public static bool TryGetExchangeId(this HttpRequestMessage request, out Guid exchangeId)
        {
            object value;
            if (request.Properties.TryGetValue(LoggingHandler.ExchangeIdPropertyKey, out value) && value is Guid)
            {
                exchangeId = (Guid) value;
                return true;
            }

            exchangeId = default(Guid);
            return false;
        }

        public static Guid GetExchangeId(this HttpRequestMessage request)
        {
            object value;
            if (request.Properties.TryGetValue(LoggingHandler.ExchangeIdPropertyKey, out value))
            {
                if (value is Guid)
                {
                    return (Guid) value;
                }

                throw new InvalidOperationException("The exchange ID found in the request is not a GUID.");
            }

            throw new KeyNotFoundException("The exchange ID could not be found on the request.");
        }
    }
}