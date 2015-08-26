using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Knapcode.SocketToMe.Socks
{
    public class Socks5Client
    {
        private const byte SocksVersion = 0x05;
        private const byte UsernamePasswordVersion = 0x01;
        private readonly byte[] _password;

        private readonly IPEndPoint _socksEndpoint;
        private readonly byte[] _username;

        public Socks5Client(IPEndPoint socksEndpoint, NetworkCredential credential = null, Encoding credentialEncoding = null)
        {
            if (socksEndpoint == null)
            {
                throw new ArgumentNullException("socksEndpoint");
            }

            _socksEndpoint = socksEndpoint;

            if (socksEndpoint.AddressFamily != AddressFamily.InterNetwork && socksEndpoint.AddressFamily != AddressFamily.InterNetworkV6)
            {
                string message = string.Format(
                    CultureInfo.InvariantCulture,
                    "The SOCKS5 endpoint address family '{0}' is not valid. The address family must either be InterNetwork (IPv4) or InterNetworkV6 (IPv6).",
                    socksEndpoint.AddressFamily);
                throw new ArgumentException(message, "socksEndpoint");
            }

            if (credential != null)
            {
                credentialEncoding = credentialEncoding ?? Encoding.UTF8;

                const string messageFormat = "The {0} in the provided credential encodes to {1} bytes. The maximum length is {2}.";
                _username = credentialEncoding.GetBytes(credential.UserName);
                if (_username.Length > byte.MaxValue)
                {
                    string message = string.Format(
                        CultureInfo.InvariantCulture,
                        messageFormat,
                        "username",
                        _username.Length,
                        byte.MaxValue);
                    throw new ArgumentException(message, "credential");
                }

                _password = credentialEncoding.GetBytes(credential.Password);
                if (_password.Length > byte.MaxValue)
                {
                    string message = string.Format(
                        CultureInfo.InvariantCulture,
                        messageFormat,
                        "password",
                        _password.Length,
                        byte.MaxValue);
                    throw new ArgumentException(message, "credential");
                }
            }
        }

        public Socket Connect(string name, int port)
        {
            ValidatePort(port, "port");

            byte[] nameBytes = Encoding.ASCII.GetBytes(name);
            byte[] addressBytes = Enumerable.Empty<byte>()
                .Concat(new[] {(byte) nameBytes.Length})
                .Concat(nameBytes)
                .ToArray();

            return Connect(AddressType.DomainName, addressBytes, port);
        }

        public Socket Connect(IPEndPoint destinationEndpoint)
        {
            ValidatePort(destinationEndpoint.Port, "destinationEndpoint");

            AddressType addressType;
            byte[] addressBytes = destinationEndpoint.Address.GetAddressBytes();

            switch (destinationEndpoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    addressType = AddressType.IpV4;
                    break;
                case AddressFamily.InterNetworkV6:
                    addressType = AddressType.IpV6;
                    break;
                default:
                    throw new ArgumentException("The destination endpoint must be an IPv4 or IPv6 address.");
            }

            return Connect(addressType, addressBytes, destinationEndpoint.Port);
        }

        private Socket Handshake(out byte[] outResponseBuffer)
        {
            // open the socket
            var socket = new Socket(_socksEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(_socksEndpoint);

            // negotiate an authentication method
            var authenticationMethods = new List<AuthenticationMethod> {AuthenticationMethod.NoAuthentication};
            if (_username != null)
            {
                authenticationMethods.Add(AuthenticationMethod.UsernamePassword);
            }

            byte[] requestBuffer = Enumerable.Empty<byte>()
                .Concat(new[] {SocksVersion, (byte) authenticationMethods.Count})
                .Concat(authenticationMethods.Select(m => (byte) m))
                .ToArray();
            socket.Send(requestBuffer);
            Console.WriteLine("SEND:    {0}", BitConverter.ToString(requestBuffer));

            var responseBuffer = new byte[socket.ReceiveBufferSize];
            outResponseBuffer = responseBuffer;
            int read = socket.Receive(responseBuffer, 0, 2, SocketFlags.None);
            Console.WriteLine("RECEIVE: {0}", BitConverter.ToString(responseBuffer, 0, read));

            if (read != 2)
            {
                socket.Close();
                string message = string.Format(
                    "The SOCKS5 proxy responded with {0} bytes, instead of 2, during the handshake.",
                    read);
                throw new Exception(message);
            }

            ValidateSocksVersion(socket, responseBuffer[0]);

            if (responseBuffer[1] == (byte) AuthenticationMethod.NoAcceptableMethods)
            {
                socket.Close();
                throw new Exception("The SOCKS5 proxy does not support any of the client's authentication methods.");
            }

            if (authenticationMethods.All(m => responseBuffer[1] != (byte) m))
            {
                socket.Close();
                string message = string.Format(
                    "The SOCKS5 proxy responded with 0x{0:x2}, which is an unexpected authentication method.",
                    responseBuffer[1]);
                throw new Exception(message);
            }

            // if username/password authentication was decided on, run the sub-negotiation
            if (responseBuffer[1] == (byte) AuthenticationMethod.UsernamePassword && _username != null)
            {
                requestBuffer = Enumerable.Empty<byte>()
                    .Concat(new[] {UsernamePasswordVersion, (byte) _username.Length})
                    .Concat(_username)
                    .Concat(new[] {(byte) _password.Length})
                    .Concat(_password)
                    .ToArray();
                socket.Send(requestBuffer, SocketFlags.None);
                Console.WriteLine("SEND:    {0}", BitConverter.ToString(requestBuffer));

                read = socket.Receive(responseBuffer, 0, 2, SocketFlags.None);
                Console.WriteLine("RECEIVE: {0}", BitConverter.ToString(responseBuffer, 0, read));

                if (read != 2)
                {
                    socket.Close();
                    string message = string.Format(
                        "The SOCKS5 proxy responded with {0} bytes, instead of 2, during the username/password authentication.",
                        read);
                    throw new Exception(message);
                }

                if (responseBuffer[0] != UsernamePasswordVersion)
                {
                    socket.Close();
                    string message = string.Format(
                        "The SOCKS5 proxy responded with 0x{0:x2}, instead of 0x{1:x2}, for the username/password authentication version number.",
                        responseBuffer[0],
                        UsernamePasswordVersion);
                    throw new Exception(message);
                }

                if (responseBuffer[1] != 0)
                {
                    socket.Close();
                    string message = string.Format(
                        "The SOCKS5 proxy responded with 0x{0:x2}, instead of 0x00, indicating a failure in username/password authentication.",
                        responseBuffer[0]);
                    throw new Exception(message);
                }


            }

            return socket;
        }

        private Socket Connect(AddressType addressType, IEnumerable<byte> addressBytes, int port)
        {
            byte[] responseBuffer;
            Socket socket = Handshake(out responseBuffer);

            byte[] requestBuffer = Enumerable.Empty<byte>()
                .Concat(new[] {SocksVersion, (byte) CommandType.Connect, (byte) 0x00, (byte) addressType})
                .Concat(addressBytes)
                .Concat(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short) port)))
                .ToArray();
            socket.Send(requestBuffer);
            Console.WriteLine("SEND:    {0}", BitConverter.ToString(requestBuffer));

            int read = socket.Receive(responseBuffer);
            Console.WriteLine("RECEIVE: {0}", BitConverter.ToString(responseBuffer, 0, read));

            if (read < 7)
            {
                socket.Close();
                string message = string.Format(
                    "The SOCKS5 proxy responded with {0} bytes to the connect request. At least 7 bytes are expected.",
                    read);
                throw new Exception(message);
            }

            ValidateSocksVersion(socket, responseBuffer[0]);

            if (responseBuffer[1] != (byte)ReplyType.Succeeded)
            {
                socket.Close();
                string message = string.Format(
                    "The SOCKS5 proxy responded with a unsuccessful reply type '{0}' (0x{1:x2}).",
                    responseBuffer[1] >= (byte) ReplyType.Unassigned ? ReplyType.Unassigned : (ReplyType) responseBuffer[1],
                    responseBuffer[1]);
                throw new Exception(message);
            }

            if (responseBuffer[2] != 0x00)
            {
                socket.Close();
                string message = string.Format(
                    "The SOCKS5 proxy responded with an unexpected reserved field value 0x{0:x2}. 0x00 was expected.",
                    responseBuffer[2]);
                throw new Exception(message);
            }

            if (!Enum.GetValues(typeof (AddressType)).Cast<byte>().Contains(responseBuffer[3]))
            {
                socket.Close();
                string message = string.Format(
                    "The SOCKS5 proxy responded with an unexpected address type 0x{0:x2}.",
                    responseBuffer[3]);
                throw new Exception(message);
            }

            object bindAddress;
            object bindPort;

            var bindAddressType = (AddressType)responseBuffer[3];
            switch (bindAddressType)
            {
                case AddressType.IpV4:
                    if (read != 10)
                    {
                        socket.Close();
                        string message = string.Format(
                            "The SOCKS5 proxy responded with an unexpected number of bytes ({0} bytes) when the address is an IPv4 address. 10 bytes were expected.",
                            read);
                        throw new Exception(message);
                    }
                    bindAddress = new IPAddress(responseBuffer.Skip(4).Take(4).ToArray());
                    bindPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(responseBuffer, 8));
                    break;

                case AddressType.DomainName:
                    byte bindAddressLength = responseBuffer[4];
                    bindAddress = Encoding.ASCII.GetString(responseBuffer, 5, bindAddressLength);
                    bindPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(responseBuffer, 5 + bindAddressLength));
                    break;

                case AddressType.IpV6:
                    if (read != 22)
                    {
                        socket.Close();
                        string message = string.Format(
                            "The SOCKS5 proxy responded with an unexpected number of bytes ({0} bytes) when the address is an IPv6 address. 22 bytes were expected.",
                            read);
                        throw new Exception(message);
                    }
                    bindAddress = new IPAddress(responseBuffer.Skip(4).Take(16).ToArray());
                    bindPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(responseBuffer, 20));
                    break;

                default:
                    socket.Close();
                    string addressTypeNotImplementedMessage = string.Format(
                        "The provided address type '{0}' is not yet implemented.",
                        bindAddressType);
                    throw new NotImplementedException(addressTypeNotImplementedMessage);
            }

            return socket;
        }

        private static void ValidateSocksVersion(Socket socket, byte version)
        {
            if (version != SocksVersion)
            {
                socket.Close();
                string message = string.Format(
                    "The SOCKS5 proxy responded with 0x{0:x2}, instead of 0x{1:x2}, for the SOCKS version number.",
                    version,
                    SocksVersion);
                throw new Exception(message);
            }
        }

        private static void ValidatePort(int port, string paramName)
        {
            if (port > ushort.MaxValue || port < 1)
            {
                string message = string.Format(
                    "The port number {0} must be a positive number less than or equal to {1}.",
                    port,
                    ushort.MaxValue);
                throw new ArgumentException(message, paramName);
            }
        }

        private enum AddressType : byte
        {
            IpV4 = 0x01,
            DomainName = 0x03,
            IpV6 = 0x04
        }

        private enum AuthenticationMethod : byte
        {
            NoAuthentication = 0x00,
            UsernamePassword = 0x02,
            NoAcceptableMethods = 0xFF
        }

        private enum CommandType : byte
        {
            Connect = 0x01
        }

        private enum ReplyType : byte
        {
            Succeeded = 0x00,
            GeneralSocksServerFailure = 0x01,
            ConnectionNotAllowedByRuleset = 0x02,
            NetworkUnreachable = 0x03,
            HostUnreachable = 0x04,
            ConnectionRefused = 0x05,
            TtlExpired = 0x06,
            CommandNotSupport = 0x07,
            AddressTypeNotSupported = 0x08,
            Unassigned = 0x09
        }
    }
}