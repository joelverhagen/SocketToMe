using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Dns.Enumerations;

namespace Knapcode.SocketToMe.Dns
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
            byte[] responseContent = await GetResponseContentAsync(request);
            return DnsProtocol.GetDnsResponseMessage(responseContent);
        }

        private async Task<byte[]> GetResponseContentAsync(DnsRequestMessage request)
        {
            // enumerate the questions
            request.Questions = request.Questions.ToArray();

            int attempts = 1;
            byte[] content = null;
            while (content == null)
            {
                // build the request
                int id = Interlocked.Increment(ref _nextId);
                byte[] requestContent = DnsProtocol.GetDnsRequestMessageBytes(id, RequestCode.Size4800, request.Questions);

                // create the socket
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, (int) Timeout.TotalMilliseconds);

                // send the request
                using (var sendArgs = new SocketAsyncEventArgs {RemoteEndPoint = new IPEndPoint(Server, Port)})
                {
                    var sendTcs = new TaskCompletionSource<bool>();
                    sendArgs.SetBuffer(requestContent, 0, requestContent.Length);
                    sendArgs.Completed += (sender, eventArgs) => sendTcs.SetResult(true);

                    if (!socket.SendToAsync(sendArgs))
                    {
                        sendTcs.SetResult(true);
                    }

                    await sendTcs.Task;
                }

                // send the response
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
                    content = new byte[read];
                    Buffer.BlockCopy(responseBuffer, 0, content, 0, read);
                }
            }

            return content;
        }
    }
}