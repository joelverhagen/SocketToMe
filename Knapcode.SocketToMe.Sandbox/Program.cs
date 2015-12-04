using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Http;
using Knapcode.SocketToMe.Http.ProtocolBuffer;
using Knapcode.SocketToMe.Socks;
using Knapcode.SocketToMe.Support;
using Knapcode.TorSharp;

namespace Knapcode.SocketToMe.Sandbox
{
    public class Program
    {
        private const int SocksPort = 9150;
        private const int HttpConnectPort = 8118;

        private static void Main()
        {
            MainAsync().Wait();
        }

        private static async Task MainAsync()
        {
            await StartTorAndPrivoxyAsync();

            Console.WriteLine("## HTTP ##");
            await HttpExampleAsync();
            Console.WriteLine();

            Console.WriteLine("## SOCKS ##");
            await SocksExampleAsync();
            Console.WriteLine();

            Console.WriteLine("## HTTPS and SOCKS ##");
            await HttpSocksExampleAsync();
            Console.WriteLine();

            Console.WriteLine("## HTTP CONNECT ##");
            await HttpConnectExampleAsync();
            Console.WriteLine();

            Console.WriteLine("## SERIALIZER ##");
            await SerializerExampleAsync();
            Console.WriteLine();
        }

        private static async Task StartTorAndPrivoxyAsync()
        {
            var settings = new TorSharpSettings { TorSocksPort = SocksPort, PrivoxyPort = HttpConnectPort };
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

        private static async Task SocksExampleAsync()
        {
            var socksEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), SocksPort);
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

        private static async Task HttpSocksExampleAsync()
        {
            var socksEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), SocksPort);
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

        private static async Task HttpConnectExampleAsync()
        {
            var socket = Tcp.ConnectToServer("127.0.0.1", HttpConnectPort);
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

        private static async Task SerializerExampleAsync()
        {
            var storeDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ProtocolBuffer");
            Directory.CreateDirectory(storeDirectory);

            var networkHandler = new NetworkHandler();
            var logger = new ProtocolBufferLogger(new FileSystemStore(storeDirectory), new HttpMessageMapper());
            var loggingHandler = new LoggingHandler(logger) { InnerHandler = networkHandler };

            using (var httpClient = new HttpClient(loggingHandler))
            {
                var requestContent = new FormUrlEncodedContent(new Dictionary<string, string> {{"foo", "1"}, {"bar", "2"}});
                var responseFromPost = await httpClient.PostAsync("http://httpbin.org/post", requestContent);
                var contentFromPost = await responseFromPost.Content.ReadAsStringAsync();
                Console.WriteLine("POST response:");
                Console.WriteLine(contentFromPost);

                var responseFromGet = httpClient.GetAsync("http://httpbin.org/ip").Result;
                var contentFromGet = await responseFromGet.Content.ReadAsStringAsync();
                Console.WriteLine("GET1 response:");
                Console.WriteLine(contentFromGet);
            }
        }
    }
}