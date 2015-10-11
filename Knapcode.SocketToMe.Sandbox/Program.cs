using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Http;
using Knapcode.SocketToMe.Socks;
using Knapcode.SocketToMe.Support;
using Knapcode.TorSharp;

namespace Knapcode.SocketToMe.Sandbox
{
    public class Program
    {
        private static void Main()
        {
            MainAsync().Wait();
        }

        private static async Task MainAsync()
        {
            int torSocksPort = 9150;
            int privoxyPort = 8118;
            await StartTorAndPrivoxyAsync(torSocksPort, privoxyPort);

            Console.WriteLine("## HTTP ##");
            await HttpExampleAsync();
            Console.WriteLine();

            Console.WriteLine("## SOCKS ##");
            await SocksExampleAsync(torSocksPort);
            Console.WriteLine();

            Console.WriteLine("## HTTPS and SOCKS ##");
            await HttpSocksExampleAsync(torSocksPort);
            Console.WriteLine();

            Console.WriteLine("## HTTP CONNECT ##");
            await HttpConnectExampleAsync(privoxyPort);
            Console.WriteLine();
        }

        private static async Task StartTorAndPrivoxyAsync(int torSocksPort, int privoxyPort)
        {
            var settings = new TorSharpSettings { TorSocksPort = torSocksPort, PrivoxyPort = privoxyPort };
            var fetcher = new TorSharpToolFetcher(settings, new HttpClient());
            await fetcher.FetchAsync();

            var proxy = new TorSharpProxy(settings);
            await proxy.ConfigureAndStartAsync();
        }
        
        private static async Task HttpExampleAsync()
        {
            using (var httpClient = new HttpClient(new NetworkHandler()))
            using (var response = await httpClient.GetAsync("http://icanhazip.com/"))
            {
                Console.WriteLine("{0} {1}", (int) response.StatusCode, response.ReasonPhrase);
                Console.WriteLine((await response.Content.ReadAsStringAsync()).Trim());
            }
        }

        private static async Task SocksExampleAsync(int torSocksPort)
        {
            // Tor support SOCKS 4, 4A, and 5
            var socksEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), torSocksPort);
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

                Console.WriteLine((await reader.ReadToEndAsync()).Trim());
            }
        }

        private static async Task HttpSocksExampleAsync(int torSocksPort)
        {
            // Tor support SOCKS 4, 4A, and 5
            var socksEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), torSocksPort);
            var socks5Client = new Socks5Client();
            var socket = socks5Client.ConnectToServer(socksEndpoint);
            socket = socks5Client.ConnectToDestination(socket, "icanhazip.com", 443);

            using (var httpClient = new HttpClient(new NetworkHandler(socket)))
            using (var response = await httpClient.GetAsync("https://icanhazip.com/"))
            {
                Console.WriteLine("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
                Console.WriteLine((await response.Content.ReadAsStringAsync()).Trim());
            }
        }

        private static async Task HttpConnectExampleAsync(int privoxyPort)
        {
            var socket = Tcp.ConnectToServer("localhost", privoxyPort);
            var httpSocketClient = new HttpSocketClient();

            var connectRequest = new HttpRequestMessage(new HttpMethod("CONNECT"), "http://icanhazip.com/");
            var connectStream = await httpSocketClient.GetStreamAsync(socket, connectRequest);
            await httpSocketClient.SendRequestAsync(connectStream, connectRequest);
            var receiveResponse = await httpSocketClient.ReceiveResponseAsync(connectStream, connectRequest);
            Console.WriteLine("{0} {1}", (int)receiveResponse.StatusCode, receiveResponse.ReasonPhrase);

            var getRequest = new HttpRequestMessage(HttpMethod.Get, "http://icanhazip.com/");
            var getStream = await httpSocketClient.GetStreamAsync(socket, getRequest);
            await httpSocketClient.SendRequestAsync(getStream, getRequest);
            var getResponse = await httpSocketClient.ReceiveResponseAsync(getStream, getRequest);
            Console.WriteLine("{0} {1}", (int)getResponse.StatusCode, getResponse.ReasonPhrase);
            Console.WriteLine((await getResponse.Content.ReadAsStringAsync()).Trim());
        }
    }
}