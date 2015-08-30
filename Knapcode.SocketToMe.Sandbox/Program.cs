using System;
using System.Net.Http;
using Knapcode.SocketToMe.Http;

namespace Knapcode.SocketToMe.Sandbox
{
    public class Program
    {
        private static void Main()
        {
            var httpClient = new HttpClient(new NetworkHandler());
            var response = httpClient.GetAsync("http://icanhazip.com/").Result;

            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
        }
    }
}