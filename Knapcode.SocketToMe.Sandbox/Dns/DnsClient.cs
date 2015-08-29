using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Sandbox.Extensions;
using Knapcode.SocketToMe.Sandbox.Dns.Enumerations;

namespace Knapcode.SocketToMe.Sandbox.Dns
{
    public class DnsClient
    {
        public DnsClient()
        {
            Port = 53;
            MaxAttempts = 5;
            Timeout = TimeSpan.FromSeconds(1);
        }

        private static int _nextId = int.MinValue;

        public IPAddress Server { get; set; }

        public int Port { get; set; }

        public int MaxAttempts { get; set; }

        public TimeSpan Timeout { get; set; }

        public async Task<DnsResponseMessage> SendAsync(DnsRequestMessage request)
        {
            byte[] responseContent = await GetResponseContentViaTcpAsync(request);

            // byte[] responseContent = await GetResponseContentViaUdpAsync(request);
            
            return DnsProtocol.GetDnsResponseMessage(responseContent);
        }

        private async Task<byte[]> GetResponseContentViaUdpAsync(DnsRequestMessage request)
        {
            // enumerate the questions
            request.Questions = request.Questions.ToArray();

            int attempts = 1;
            byte[] responseContent = null;
            while (responseContent == null)
            {
                // build the request
                int id = Interlocked.Increment(ref _nextId);
                byte[] requestContent = DnsProtocol.GetDnsRequestMessageBytes(id, RequestCode.Size4800, request.Questions);

                // create the socket
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // send the request
                await socket.SendToAsync(requestContent, 0, requestContent.Length, SocketFlags.None, new IPEndPoint(Server, Port));

                // read the response
                // TODO: use better async overloads
                using (var receiveArgs = new SocketAsyncEventArgs())
                {
                    var recieveTcs = new TaskCompletionSource<int>();
                    var responseBuffer = new byte[4800];
                    receiveArgs.SetBuffer(responseBuffer, 0, responseBuffer.Length);
                    receiveArgs.Completed += (sender, eventArgs) => recieveTcs.SetResult(eventArgs.BytesTransferred);

                    if (!socket.ReceiveAsync(receiveArgs))
                    {
                        recieveTcs.SetResult(receiveArgs.BytesTransferred);
                    }

                    var wait = await Task.WhenAny(recieveTcs.Task, Task.Delay(Timeout));
                    if (wait != recieveTcs.Task)
                    {
                        if (attempts < MaxAttempts)
                        {
                            attempts++;
                            continue;
                        }

                        throw new SocketException((int)SocketError.TimedOut);
                    }

                    int read = await recieveTcs.Task;
                    responseContent = new byte[read];
                    Buffer.BlockCopy(responseBuffer, 0, responseContent, 0, read);
                }
            }

            return responseContent;
        }

        private async Task<byte[]> GetResponseContentViaTcpAsync(DnsRequestMessage request)
        {
            // enumerate the questions
            request.Questions = request.Questions.ToArray();
            
            // create the socket
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // build the request
            int id = Interlocked.Increment(ref _nextId);
            byte[] requestContent = DnsProtocol.GetDnsRequestMessageBytes(id, RequestCode.Size512, request.Questions);
            byte[] requestContentWithLength = Enumerable.Empty<byte>()
                .Concat(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)requestContent.Length)))
                .Concat(requestContent)
                .ToArray();

            // connect
            await WithSocketTimeout(socket.ConnectAsync(new IPEndPoint(Server, Port)), Timeout);

            // send the request
            await SendExactAsync(socket, requestContentWithLength);

            // read the response
            var responseLengthBuffer = new byte[2];
            await WithSocketTimeout(ReceiveExactAsync(socket, responseLengthBuffer), Timeout);
            var responseLength = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(responseLengthBuffer, 0));

            var responseContent = new byte[responseLength];
            await ReceiveExactAsync(socket, responseContent);

            return responseContent;
        }

        private static async Task WithSocketTimeout(Task task, TimeSpan timeout)
        {
            try
            {
                await task.WithTimeout(timeout);
            }
            catch (TimeoutException)
            {
                throw new SocketException((int)SocketError.TimedOut);
            }
        }

        private static async Task ReceiveExactAsync(Socket socket, byte[] buffer)
        {
            int received = await socket.ReceiveAsync(buffer, 0, buffer.Length, SocketFlags.None);
            if (received != buffer.Length)
            {
                string message = String.Format(
                    CultureInfo.InvariantCulture,
                    "The DNS response did not  contain the correct number of bytes. {0} byte{1} received when {2} byte{3} expected.",
                    received,
                    received == 1 ? " was" : "s were",
                    buffer.Length,
                    buffer.Length == 1 ? " was" : "s were");
                throw new DnsResponseException(message);
            }
        }

        private static async Task SendExactAsync(Socket socket, byte[] buffer)
        {
            int sent = await socket.SendAsync(buffer, 0, buffer.Length, SocketFlags.None);
            if (sent != buffer.Length)
            {
                string message = String.Format(
                    CultureInfo.InvariantCulture,
                    "The number of bytes sent in the DNS request was not correct. {0} byte{1} sent when {2} byte{3} expected.",
                    sent,
                    sent == 1 ? " was" : "s were",
                    buffer.Length,
                    buffer.Length == 1 ? " was" : "s were");
                throw new DnsResponseException(message);
            }
        }
    }
}