using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Knapcode.SocketToMe.Socks;

namespace Knapcode.SocketToMe.Sandbox
{
    public class Program
    {
        private static void Main()
        {
            // Tor support SOCKS 4, 4A, and 5
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9150);

            var client = new Socks5Client();
            // var client = new Socks4Client();

            var socket = client.ConnectToServer(endpoint);
            socket = client.ConnectToDestination(socket, new IPEndPoint(IPAddress.Parse("104.238.136.31"), 443));

            using (var proxiedStream = new NetworkStream(socket))
            using (var sslStream = new SslStream(proxiedStream))
            {
                sslStream.AuthenticateAsClient("icanhazip.com");

                using (var writer = new StreamWriter(sslStream))
                using (var reader = new StreamReader(sslStream))
                {
                    writer.WriteLine("GET / HTTP/1.1");
                    writer.WriteLine("Host: icanhazip.com");
                    writer.WriteLine();
                    writer.Flush();

                    Console.WriteLine(reader.ReadToEnd());
                }
            }
        }
    }
}