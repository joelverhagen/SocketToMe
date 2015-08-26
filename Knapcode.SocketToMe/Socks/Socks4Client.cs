using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Knapcode.SocketToMe.Socks
{
    public class Socks4Client
    {
        private const byte SocksVersion = 0x04;
        private const byte ReplyVersion = 0x00;
        private static readonly byte[] NullTerminator = {0x0};

        private readonly IPEndPoint _socksEndpoint;
        private readonly byte[] _userId;

        public Socks4Client(IPEndPoint socksEndpoint, string userId = null, Encoding userdIdEncoding = null)
        {
            if (socksEndpoint == null)
            {
                throw new ArgumentNullException(nameof(socksEndpoint));
            }

            _socksEndpoint = socksEndpoint;

            if (socksEndpoint.AddressFamily != AddressFamily.InterNetwork)
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "The SOCKS4 endpoint address family '{0}' is not valid. The address family be InterNetwork (IPv4).",
                    socksEndpoint.AddressFamily);
                throw new ArgumentException(message, nameof(socksEndpoint));
            }

            if (userId != null)
            {
                userdIdEncoding = userdIdEncoding ?? Encoding.UTF8;
                _userId = userdIdEncoding.GetBytes(userId);
            }
            else
            {
                _userId = new byte[0];
            }
        }

        public Socket Connect(string name, int port)
        {
            ValidatePort(port, nameof(port));

            var connectBytes = GetConnectBytes(new IPEndPoint(IPAddress.Parse("0.0.0.1"), port))
                .Concat(Encoding.ASCII.GetBytes(name))
                .Concat(NullTerminator);
            
            return Connect(connectBytes);
        }

        public Socket Connect(IPEndPoint destinationEndpoint)
        {
            ValidatePort(destinationEndpoint.Port, nameof(destinationEndpoint));

            if (destinationEndpoint.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new ArgumentException("The destination endpoint must be an IPv4 address.", nameof(destinationEndpoint));
            }

            return Connect(GetConnectBytes(destinationEndpoint));
        }

        private IEnumerable<byte> GetConnectBytes(IPEndPoint destinationEndpoint)
        {
            return Enumerable.Empty<byte>()
                .Concat(new[] {SocksVersion, (byte) CommandType.Connect})
                .Concat(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) destinationEndpoint.Port)))
                .Concat(destinationEndpoint.Address.GetAddressBytes())
                .Concat(_userId)
                .Concat(new[] {(byte) 0x0});
        }

        private Socket Connect(IEnumerable<byte> connectBytes)
        {
            // open the socket
            var socket = new Socket(_socksEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(_socksEndpoint);

            // send the connect request
            byte[] requestBuffer = connectBytes.ToArray();
            socket.Send(requestBuffer);
            Console.WriteLine("SEND:    {0}", BitConverter.ToString(requestBuffer));

            var responseBuffer = new byte[8];
            int read = socket.Receive(responseBuffer);
            Console.WriteLine("RECEIVE: {0}", BitConverter.ToString(responseBuffer, 0, read));

            // validate the response
            if (read != responseBuffer.Length)
            {
                socket.Close();
                string message = string.Format(
                    "The SOCKS4 proxy responded with {0} bytes to the connect request. Exactly 8 bytes are expected.",
                    read);
                throw new Exception(message);
            }

            if (responseBuffer[0] != ReplyVersion)
            {
                socket.Close();
                string message = string.Format(
                    "The first byte returned by the SOCKS4 proxy was {0:x2}, not a null byte.",
                    responseBuffer[0]);
                throw new Exception(message);
            }

            if (!Enum.IsDefined(typeof (ReplyType), responseBuffer[1]))
            {
                socket.Close();
                string message = string.Format(
                    "The reply type {0x2} returned by the server is not recognized.",
                    responseBuffer[1]);
                throw new Exception(message);
            }

            var replyType = (ReplyType)responseBuffer[1];
            if (replyType != ReplyType.RequestGranted)
            {
                socket.Close();
                string message = string.Format(
                    "The SOCKS4 request was not granted. The reply type was {0} ({1x2})",
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
                string message = string.Format(
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