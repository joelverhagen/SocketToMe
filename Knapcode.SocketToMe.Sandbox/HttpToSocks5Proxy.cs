using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.SocketToMe.Sandbox
{
    public class HttpToSocks5Proxy
    {
        private readonly IPAddress _listenAddress;
        private readonly int _listenPort;
        private TcpListener _listener;
        private bool _listening;
        private Thread _thread;

        public HttpToSocks5Proxy(IPAddress listenAddress, int listenPort)
        {
            _listenAddress = listenAddress;
            _listenPort = listenPort;
            _listening = false;
        }

        private void Listen()
        {
            _listener = new TcpListener(_listenAddress, _listenPort);
            _listener.Start();
            _listening = true;
            while (_listening)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Task.Run(() => HandleClient(client));
                }
                catch (SocketException e)
                {
                    if (e.ErrorCode == 10004)
                    {
                        return;
                    }

                    throw;
                }

            }
        }

        public void Start()
        {
            _thread = new Thread(Listen);
            _thread.Start();
        }

        public void Stop()
        {
            _listening = false;
            _listener.Stop();
            _thread.Abort();
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream ns = client.GetStream();
            
            var buffer = new byte[1024];
            int read = ns.Read(buffer, 0, buffer.Length);
            Console.WriteLine(Encoding.ASCII.GetString(buffer, 0, read));

            byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 4\r\n\r\nanna");
            ns.Write(response, 0, response.Length);

            ns.Close();
            client.Close();
        }
    }
}