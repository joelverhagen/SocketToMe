# SocketToMe

Fun with sockets in C#.

## Features

- Connect to a SOCKS proxy server
  - SOCKS 4 and 4A: TCP CONNECT
  - SOCKS 5: TCP CONNECT with optional username and password authentication
- Connect to an HTTP server without WinHTTP (e.g. HttpClientHandler)
  - Use the custom delegating handler `NetworkHandler` with `HttpClient`
  - HTTP/1.1
  - HTTPS
  - Chunked responses
  - Most of the features provided by `HttpClient`
  - Arbitrary sockets (e.g. connected to a SOCKS proxy server!)
  - Automatic redirects (with `RedirectingHandler`)
  - Automatic decompression (with `DecompressingHandler`)
  - Cookies (with `CookieHandler`)
  - HTTP CONNECT

There's probably a lot of bugs with `NetworkHandler`... it's not very thoroughly tested.

## Install

```
Install-Package Knapcode.SocketToMe
```

## Examples

### HTTP

```csharp
using (var httpClient = new HttpClient(new NetworkHandler()))
using (var response = await httpClient.GetAsync("http://icanhazip.com/"))
{
    Console.WriteLine("{0} {1}", (int) response.StatusCode, response.ReasonPhrase);
    Console.WriteLine((await response.Content.ReadAsStringAsync()).Trim());
}
```

### SOCKS

Talk to a website through Tor! Have [Tor Browser](https://www.torproject.org/download/download-easy.html.en) running at the same time to try this demo out. Port 9150 is the default port for Tor Browser.

```csharp
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
```

### HTTPS and SOCKS

```csharp
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
```

### HTTP CONNECT

```csharp
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
```

## TODO

- SOCKS
  - `async` support to `Socks4Client` and `Socks5Client`
- HTTP
  - Client certificates
  - HTTP/1.0
  - Chunked requests
  - HTTP and HTTPS proxies
  - Connection pools (Keep-Alive)
- Everywhere
  - Better cancellation token support
