using System;
using System.Net.Http;

namespace Knapcode.SocketToMe.Http
{
    public enum RedirectEventType
    {
        InitialRequest,
        RedirectResponse,
        RedirectRequest,
        FinalResponse
    }

    public class RedirectEventArgs : EventArgs
    {
        public RedirectEventArgs(RedirectEventType type, Guid redirectId, Guid exchangeId, HttpRequestMessage request) : this(type, redirectId, exchangeId, request, null)
        {
        }

        public RedirectEventArgs(RedirectEventType type, Guid redirectId, Guid exchangeId, HttpResponseMessage response) : this(type, redirectId, exchangeId, null, response)
        {
        }

        private RedirectEventArgs(RedirectEventType type, Guid redirectId, Guid exchangeId, HttpRequestMessage request, HttpResponseMessage response)
        {
            Type = type;
            RedirectId = redirectId;
            ExchangeId = exchangeId;
            Request = request;
            Response = response;
        }

        public RedirectEventType Type { get; }
        public Guid RedirectId { get; }
        public Guid ExchangeId { get; }
        public HttpRequestMessage Request { get; }
        public HttpResponseMessage Response { get; }
    }
}