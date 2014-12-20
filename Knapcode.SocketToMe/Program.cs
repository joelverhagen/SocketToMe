using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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

            IPAddress address = IPAddress.Parse("::1");
            var endpoint = new IPEndPoint(address, 1337);
            var client = new Socks5Client(endpoint, new NetworkCredential("foo", "bar"));
            // Socket socket = client.Connect("v6.ipv6-test.com", 80);
            Socket socket = client.Connect(new IPEndPoint(IPAddress.Parse("5.135.165.173"), 80));
            // Socket socket = client.Connect(new IPEndPoint(IPAddress.Parse("2001:41d0:8:e8ad::1"), 80));

            using (var proxiedStream = new NetworkStream(socket))
            using (var writer = new StreamWriter(proxiedStream, Encoding.ASCII))
            using (var reader = new StreamReader(proxiedStream, Encoding.ASCII))
            {
                writer.WriteLine("GET /api/myip.php HTTP/1.1");
                writer.WriteLine("Host: v4.ipv6-test.com");
                writer.WriteLine();
                writer.Flush();

                var responseBuffer = new byte[socket.ReceiveBufferSize];
                int read = proxiedStream.Read(responseBuffer, 0, responseBuffer.Length);
                Console.WriteLine(Encoding.ASCII.GetString(responseBuffer, 0, read));
            }
        }
    }

    /*
    public class HttpClient
    {
        public byte[] Get(Uri requestUri)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(requestUri.DnsSafeHost);
            IPAddress address = hostEntry.AddressList.First();

            var rhost = new IPEndPoint(address, requestUri.Port);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(rhost);

            byte[] request = Encoding.ASCII.GetBytes(string.Format(
                CultureInfo.InvariantCulture,
                "GET {0} HTTP/1.1\r\nHost: {1}\r\n\r\n",
                requestUri.PathAndQuery,
                requestUri.Host));
            socket.Send(request, SocketFlags.None);

            var response = new MemoryStream();
            var buffer = new byte[socket.ReceiveBufferSize];
            int recieved;
            do
            {
                recieved = socket.Receive(buffer);
                response.Write(buffer, 0, recieved);
            } while (recieved == buffer.Length);
            
            socket.Close();

            return response.ToArray();
        }
    }
    */
}