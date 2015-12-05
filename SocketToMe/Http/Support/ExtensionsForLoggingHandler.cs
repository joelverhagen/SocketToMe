using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Knapcode.SocketToMe.Http
{
    public static class ExtensionsForLoggingHandler
    {
        public static bool TryGetExchangeId(this HttpRequestMessage request, out ExchangeId exchangeId)
        {
            object value;
            if (request.Properties.TryGetValue(LoggingHandler.ExchangeIdPropertyKey, out value) && value is ExchangeId)
            {
                exchangeId = (ExchangeId) value;
                return true;
            }

            exchangeId = default(ExchangeId);
            return false;
        }

        public static ExchangeId GetExchangeId(this HttpRequestMessage request)
        {
            object value;
            if (request.Properties.TryGetValue(LoggingHandler.ExchangeIdPropertyKey, out value))
            {
                if (value is ExchangeId)
                {
                    return (ExchangeId) value;
                }

                throw new InvalidOperationException("The exchange ID value found in the request is not the correct type.");
            }

            throw new KeyNotFoundException("The exchange ID could not be found on the request.");
        }
    }
}