using System;
using System.Net;
using System.Net.Http;
using Knapcode.SocketToMe.Http;
using Knapcode.SocketToMe.Socks;

namespace Knapcode.SocketToMe.Sandbox
{
    public class Program
    {
        private static void Main()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9150);
            var client = new Socks5Client();

            var socket = client.ConnectToServer(endpoint);
            socket = client.ConnectToDestination(socket, "icanhazip.com", 443);

            var httpClient = new HttpClient(new NetworkHandler(socket));
            var response = httpClient.GetAsync("https://icanhazip.com/").Result;

            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
        }
    }
}