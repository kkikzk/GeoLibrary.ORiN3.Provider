using ORiN3.Provider.Core;
using System.Net;
using System.Net.Sockets;

namespace GeoLibrary.ORiN3.Provider.BaseLib.Net.Http;

public static class HttpClientEx
{
    public static HttpClient Create(string localIpAddress, string proxyUri)
    {
        var proxy = new WebProxy
        {
            Address = new Uri(proxyUri),
            BypassProxyOnLocal = false,
            UseDefaultCredentials = false
        };

        var handler = CreateHandler(localIpAddress);
        handler.Proxy = proxy;
        handler.UseProxy = true;
        return new HttpClient(handler);
    }

    public static HttpClient Create(string localIpAddress)
    {
        var handler = CreateHandler(localIpAddress);
        return new HttpClient(handler);
    }

    private static SocketsHttpHandler CreateHandler(string localIpAddress)
    {
        return new SocketsHttpHandler
        {
            ConnectCallback = async (context, token) =>
            {
                if (!IPAddress.TryParse(localIpAddress, out var parsedLocalIpAddress))
                {
                    throw new ArgumentException($"Invalid IP address format: {localIpAddress}", nameof(localIpAddress));
                }

                var dnsAddresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, token).ConfigureAwait(false);
                var remoteIp = dnsAddresses.FirstOrDefault(ip => ip.AddressFamily == parsedLocalIpAddress.AddressFamily)
                    ?? throw new InvalidOperationException($"No matching IP address found for {context.DnsEndPoint.Host}.");
                var socket = new Socket(parsedLocalIpAddress.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                var connected = false;
                try
                {
                    socket.Bind(new IPEndPoint(parsedLocalIpAddress, port: 0));
                    ORiN3ProviderLogger.LogTrace($"Trying to connect to {remoteIp}");
                    await socket.ConnectAsync(remoteIp, context.DnsEndPoint.Port, token).ConfigureAwait(false);
                    connected = true;
                    ORiN3ProviderLogger.LogTrace($"Connected to {remoteIp}");
                    return new NetworkStream(socket, ownsSocket: true);
                }
                finally
                {
                    if (!connected)
                    {
                        try
                        {
                            socket.Dispose();
                        }
                        catch
                        {
                            // nothing to do.
                        }
                    }
                }
            }
        };
    }
}
