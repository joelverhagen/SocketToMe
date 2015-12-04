using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Knapcode.SocketToMe.Support;

namespace Knapcode.SocketToMe.Socks
{
    public class Socks4Client
    {
        private const byte SocksVersion = 0x04;
        private const byte ReplyVersion = 0x00;
        private static readonly byte[] NullTerminator = {0x0};

        public Socket ConnectToServer(IPEndPoint endpoint)
        {
            return Tcp.ConnectToServer(endpoint, new[] {AddressFamily.InterNetwork});
        }

        public Socket ConnectToDestination(Socket socket, string name, int port, string userId = null, Encoding userIdEncoding = null)
        {
            ValidatePort(port, nameof(port));

            var connectBytes = GetConnectBytes(new IPEndPoint(IPAddress.Parse("0.0.0.1"), port), userId, userIdEncoding)
                .Concat(Encoding.ASCII.GetBytes(name))
                .Concat(NullTerminator);

            return ConnectToDestination(socket, connectBytes);
        }

        public Socket ConnectToDestination(Socket socket, IPEndPoint endpoint, string userId = null, Encoding userIdEncoding = null)
        {
            ValidatePort(endpoint.Port, nameof(endpoint));
            if (endpoint.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new ArgumentException("The destination endpoint must be an IPv4 address.", nameof(endpoint));
            }

            var connectBytes = GetConnectBytes(endpoint, userId, userIdEncoding);

            return ConnectToDestination(socket, connectBytes);
        }

        private IEnumerable<byte> GetConnectBytes(IPEndPoint endpoint, string userId, Encoding userIdEncoding)
        {
            userId = userId ?? string.Empty;
            userIdEncoding = userIdEncoding ?? Encoding.UTF8;

            return Enumerable.Empty<byte>()
                .Concat(new[] {SocksVersion, (byte) CommandType.Connect})
                .Concat(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) endpoint.Port)))
                .Concat(endpoint.Address.GetAddressBytes())
                .Concat(userIdEncoding.GetBytes(userId))
                .Concat(NullTerminator);
        }

        private Socket ConnectToDestination(Socket socket, IEnumerable<byte> connectBytes)
        {
            // send the connect request
            var requestBuffer = connectBytes.ToArray();
            socket.Send(requestBuffer);

            var responseBuffer = new byte[8];
            var read = socket.Receive(responseBuffer);

            // validate the response
            if (read != responseBuffer.Length)
            {
                socket.Close();
                var message = string.Format(
                    "The SOCKS4 proxy responded with {0} bytes to the connect request. Exactly 8 bytes are expected.",
                    read);
                throw new Exception(message);
            }

            if (responseBuffer[0] != ReplyVersion)
            {
                socket.Close();
                var message = string.Format(
                    "The first byte returned by the SOCKS4 proxy was {0:x2}, not a null byte.",
                    responseBuffer[0]);
                throw new Exception(message);
            }

            if (!Enum.IsDefined(typeof (ReplyType), responseBuffer[1]))
            {
                socket.Close();
                var message = string.Format(
                    "The reply type {0x2} returned by the server is not recognized.",
                    responseBuffer[1]);
                throw new Exception(message);
            }

            var replyType = (ReplyType) responseBuffer[1];
            if (replyType != ReplyType.RequestGranted)
            {
                socket.Close();
                var message = string.Format(
                    "The SOCKS4 request was not granted. The reply type was {0} ({1:x2})",
                    replyType,
                    responseBuffer[1]);
                throw new Exception(message);
            }

            return socket;
        }

        private static void ValidatePort(int port, string paramName)
        {
            if (port > ushort.MaxValue || port < 1)
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "The port number {0} must be a positive number less than or equal to {1}.",
                    port,
                    ushort.MaxValue);
                throw new ArgumentException(message, paramName);
            }
        }

        private enum CommandType : byte
        {
            Connect = 0x01
        }

        private enum ReplyType : byte
        {
            RequestGranted = 0x5A,
            RequestRejectedOrFailed = 0x5B,
            RequestRejectedDueToIdent = 0x5C,
            RequestRejectedDueToIdentMismatch = 0x5D
        }
    }
}