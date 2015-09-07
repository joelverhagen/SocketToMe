using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Knapcode.SocketToMe.Http;
using Knapcode.SocketToMe.Socks;

namespace Knapcode.SocketToMe.Sandbox
{
    public class Program
    {
        private static void Main()
        {
            Console.WriteLine("## HTTP ##");
            HttpExample();
            Console.WriteLine();

            Console.WriteLine("## SOCKS ##");
            SocksExample();
            Console.WriteLine();

            Console.WriteLine("## HTTP and SOCKS ##");
            HttpSocksExample();
            Console.WriteLine();
        }
        
        private static void HttpExample()
        {
            using (var httpClient = new HttpClient(new NetworkHandler()))
            using (var response = httpClient.GetAsync("http://icanhazip.com/").Result)
            {
                Console.WriteLine("{0} {1}", (int) response.StatusCode, response.ReasonPhrase);
                Console.WriteLine(response.Content.ReadAsStringAsync().Result.Trim());
            }
        }

        private static void SocksExample()
        {
            // Tor support SOCKS 4, 4A, and 5
            var socksEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9150);
            var socks5Client = new Socks5Client();
            var socket = socks5Client.ConnectToServer(socksEndpoint);
            socket = socks5Client.ConnectToDestination(socket, "icanhazip.com", 80);

            using (var proxiedStream = new NetworkStream(socket))
            using (var writer = new StreamWriter(proxiedStream))
            using (var reader = new StreamReader(proxiedStream))
            {
                writer.WriteLine("GET / HTTP/1.1");
                writer.WriteLine("Host: icanhazip.com");
                writer.WriteLine();
                writer.Flush();

                Console.WriteLine(reader.ReadToEnd().Trim());
            }
        }

        private static void HttpSocksExample()
        {
            // Tor support SOCKS 4, 4A, and 5
            var socksEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9150);
            var socks5Client = new Socks5Client();
            var socket = socks5Client.ConnectToServer(socksEndpoint);
            socket = socks5Client.ConnectToDestination(socket, "icanhazip.com", 443);

            using (var httpClient = new HttpClient(new NetworkHandler(socket)))
            using (var response = httpClient.GetAsync("https://icanhazip.com/").Result)
            {
                Console.WriteLine("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
                Console.WriteLine(response.Content.ReadAsStringAsync().Result.Trim());
            }
        }
    }
}