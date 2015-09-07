# SocketToMe

Fun with sockets in C#.

## Details

- Connect to a SOCKS proxy server
  - SOCKS 4 and 4A: TCP CONNECT
  - SOCKS 5: TCP CONNECT with optional username and password authentication
- Connect to an HTTP server without WinHTTP (e.g. HttpClientHandler)
  - Use the custom delegating handler `NetworkHandler` with `HttpClient`
  - HTTP/1.1
  - Chunked responses
  - Most of the features provided by `HttpClient`
  - Arbitrary sockets (e.g. connected to a SOCKS proxy server!)

There's probably a lot of bugs with `NetworkHandler`... it's not very thoroughly tested.

## Install

```
Install-Package Knapcode.SocketToMe
```

## Examples

Talk to a website through Tor! Have [Tor Browser](https://www.torproject.org/download/download-easy.html.en) running at the same time to try this demo out. Port 9150 is the default port for Tor Browser.

### HTTP Example

```csharp
using (var httpClient = new HttpClient(new NetworkHandler()))
using (var response = httpClient.GetAsync("http://icanhazip.com/").Result)
{
    Console.WriteLine("{0} {1}", (int) response.StatusCode, response.ReasonPhrase);
    Console.WriteLine(response.Content.ReadAsStringAsync().Result.Trim());
}
```

### SOCKS Example

```csharp
// Tor support SOCKS 4, 4A, and 5
var socksEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9150);
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

    Console.WriteLine(reader.ReadToEnd().Trim());
}
```

### HTTP + SOCKS

```csharp
// Tor support SOCKS 4, 4A, and 5
var socksEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9150);
var socks5Client = new Socks5Client();
var socket = socks5Client.ConnectToServer(socksEndpoint);
socket = socks5Client.ConnectToDestination(socket, "icanhazip.com", 443);

using (var httpClient = new HttpClient(new NetworkHandler(socket)))
using (var response = httpClient.GetAsync("https://icanhazip.com/").Result)
{
    Console.WriteLine("{0} {1}", (int)response.StatusCode, response.ReasonPhrase);
    Console.WriteLine(response.Content.ReadAsStringAsync().Result.Trim());
}
```

## Because I am forgetful

- SOCKS
  - Add `async` support to `Socks4Client` and `Socks5Client`
- HTTP
  - Support client certificates
  - Support HTTP/1.0
  - Support chunked requests
  - Gzip, deflate, etc.
- Everywhere
  - Add better cancellation token support
