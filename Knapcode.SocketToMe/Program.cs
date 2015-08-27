using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Knapcode.SocketToMe.Dns;
using Knapcode.SocketToMe.Dns.Enumerations;
using Knapcode.SocketToMe.Socks;

namespace Knapcode.SocketToMe
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // ======== SOCKS ========
            {
                var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9150); // SOCKS4
                // var endpoint = new IPEndPoint(IPAddress.Parse("37.187.35.186"), 14555); // SOCKS5 (seems to not support IPV6)
                // var endpoint = new IPEndPoint(IPAddress.Parse("67.201.33.70"), 9100); // SOCKS5 (seems to support IPV6)

                // var client = new Socks5Client(endpoint);
                var client = new Socks4Client();
                var socketA = client.ConnectToServer(endpoint);
                socketA = client.ConnectToDestination(socketA, new IPEndPoint(IPAddress.Parse("195.91.191.59"), 1080));
                socketA = client.ConnectToDestination(socketA, new IPEndPoint(IPAddress.Parse("175.140.137.133"), 1080));
                socketA = client.ConnectToDestination(socketA, new IPEndPoint(IPAddress.Parse("104.238.136.31"), 80));

                // socket = client.ConnectToDestination(socket, "icanhazip.com", 80);
                // socket = client.ConnectToDestination(socket, new IPEndPoint(IPAddress.Parse("2001:19f0:9000:8945::31"), 80));


                using (var proxiedStream = new NetworkStream(socketA))
                using (var writer = new StreamWriter(proxiedStream, Encoding.ASCII))
                using (var reader = new StreamReader(proxiedStream, Encoding.ASCII))
                {
                    writer.WriteLine("GET / HTTP/1.1");
                    writer.WriteLine("Host: icanhazip.com");
                    writer.WriteLine();
                    writer.Flush();

                    var responseBuffer = new byte[socketA.ReceiveBufferSize];
                    int read = proxiedStream.Read(responseBuffer, 0, responseBuffer.Length);
                    Console.WriteLine(Encoding.ASCII.GetString(responseBuffer, 0, read));
                }
            }

            return;

            // ======== HTTP ========
            {
                Func<HttpRequestMessage> getRequest = () => new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("http://www.httpbin.org/post"),
                    Content = new StringContent("1", Encoding.UTF8, "application/json")
                };
                
                var normalClient = new HttpClient();
                var normalResponse = normalClient.SendAsync(getRequest()).Result;
                Console.WriteLine(normalResponse.Content.ReadAsStringAsync().Result);

                var customClient = new HttpClient(new CustomHandler());
                var customResponse = customClient.SendAsync(getRequest()).Result;
                Console.WriteLine(customResponse.Content.ReadAsStringAsync().Result);
            }
            
            return;

            // ======== DNS ========
            {
                var dnsServers = new[]
                {
                    IPAddress.Parse("8.8.4.4"), // Google   
                    IPAddress.Parse("8.8.8.8"), // Google
                    IPAddress.Parse("208.67.220.220"), // OpenDNS Home
                    IPAddress.Parse("208.67.222.222"), // OpenDNS Home
                    IPAddress.Parse("89.233.43.71"), // censurfridns.dk
                    IPAddress.Parse("91.239.100.100"), // censurfridns.dk
                    IPAddress.Parse("8.20.247.20"), // Comodo Secure DNS
                    IPAddress.Parse("8.26.56.26"), // Comodo Secure DNS
                    IPAddress.Parse("156.154.70.1"), // DNS Advantage
                    IPAddress.Parse("156.154.71.1"), // DNS Advantage
                    IPAddress.Parse("84.200.69.80"), // DNS.WATCH
                    IPAddress.Parse("84.200.70.40"), // DNS.WATCH
                    IPAddress.Parse("216.146.35.35"), // Dyn
                    IPAddress.Parse("216.146.36.36"), // Dyn
                    IPAddress.Parse("37.235.1.174"), // FreeDNS
                    IPAddress.Parse("37.235.1.177"), // FreeDNS
                    IPAddress.Parse("209.88.198.133"), // GreenTeamDNS
                    IPAddress.Parse("81.218.119.11"), // GreenTeamDNS
                    IPAddress.Parse("209.244.0.3"), // Level3
                    IPAddress.Parse("209.244.0.4"), // Level3
                    IPAddress.Parse("199.85.126.10"), // Norton ConnectSafe
                    IPAddress.Parse("199.85.127.10"), // Norton ConnectSafe
                    IPAddress.Parse("208.115.243.35"), // OpenNIC
                    IPAddress.Parse("216.87.84.211"), // OpenNIC
                    IPAddress.Parse("199.5.157.131"), // Public-Root
                    IPAddress.Parse("208.71.35.137"), // Public-Root
                    IPAddress.Parse("195.46.39.39"), // SafeDNS
                    IPAddress.Parse("195.46.39.40"), // SafeDNS
                    IPAddress.Parse("208.76.50.50"), // SmartViper
                    IPAddress.Parse("208.76.51.51") // SmartViper
                };

                var failures = new HashSet<IPAddress>();

                using (var stream = new FileStream("alexa_top_500_2014_12_07.txt", FileMode.Open))
                using (var streamReader = new StreamReader(stream))
                {
                    string name;
                    int rank = 0;
                    while ((name = streamReader.ReadLine()) != null)
                    {
                        name = name.Trim().ToLower();
                        if (name == string.Empty)
                        {
                            continue;
                        }
                        rank++;

                        foreach (IPAddress dnsServer in dnsServers)
                        {
                            if (failures.Contains(dnsServer) || dnsServer.ToString() != "156.154.71.1")
                            {
                                continue;
                            }

                            Console.Write("{0},{1},{2}", rank, name, dnsServer);

                            var dnsClient = new DnsClient
                            {
                                Server = dnsServer,
                                MaxAttempts = 2
                            };

                            var request = new DnsRequestMessage
                            {
                                Questions = new[] {new Question {Name = name, Type = QuestionType.All, Class = QuestionClass.In}}
                            };

                            try
                            {
                                dnsClient.SendAsync(request).Wait();
                                Console.WriteLine(",success");
                            }
                            catch (AggregateException ae)
                            {
                                var e = ae.Flatten().InnerException as SocketException;
                                if (e != null)
                                {
                                    failures.Add(dnsServer);
                                    Console.WriteLine("," + e.Message);
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }

                return;
            }
        }
    }
}