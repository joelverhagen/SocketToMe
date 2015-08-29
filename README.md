# SocketToMe

Fun with sockets in C#.

## Details

- Connect to a SOCKS proxy server
  - SOCKS 4 and 4A: TCP CONNECT
  - SOCKS 5: TCP CONNECT with optional username and password authentication

Don't get too excited. You have to write your own HTTP stack if you want to send HTTP traffic over SOCKS. .NET Framework has no support for swapping out transport layer details (e.g. a TCP socket) in their HTTP clients. Thanks a lot WinHTTP...

## Install

```
Install-Package Knapcode.SocketToMe
```

## Example

Talk to a website through Tor! Have [Tor Browser](https://www.torproject.org/download/download-easy.html.en) running at the same time to try this demo out. Port 9150 is the default port for Tor Browser.

```csharp
// Tor support SOCKS 4, 4A, and 5
var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9150);

// var client = new Socks4Client();
var client = new Socks5Client();

var socket = client.ConnectToServer(endpoint);
socket = client.ConnectToDestination(socket, new IPEndPoint(IPAddress.Parse("104.238.136.31"), 443));

using (var proxiedStream = new NetworkStream(socket))
using (var sslStream = new SslStream(proxiedStream))
{
    sslStream.AuthenticateAsClient("icanhazip.com");

    using (var writer = new StreamWriter(sslStream))
    using (var reader = new StreamReader(sslStream))
    {
        writer.WriteLine("GET / HTTP/1.1");
        writer.WriteLine("Host: icanhazip.com");
        writer.WriteLine();
        writer.Flush();

        Console.WriteLine(reader.ReadToEnd());
    }
}
```
