using Design.ORiN3.Provider.Core.V1;
using System.Diagnostics;
using System.Net;

namespace GeoLibrary.ORiN3.Provider.TestBaseLib.Argument;

[DebuggerStepThrough]
public class ProviderCoreStartupArgumentEndpointInfo(int protocolType, string host,
    int port, byte[] pfx, string pfxPassword, byte[] reserved, bool clientAuthenticationEnabled) : IProviderCoreStartupArgumentEndpointInfo
{
    public int ProtocolType { get; } = protocolType;
    public string Host { get; } = host;
    public int Port { get; } = port;
    public byte[] Pfx { get; } = pfx;
    public string PfxPassword { get; } = pfxPassword;
    public byte[] Reserved { get; } = reserved;
    public bool ClientAuthenticationEnabled { get; } = clientAuthenticationEnabled;

    public ProviderCoreStartupArgumentEndpointInfo(int protocolType, int portNumber)
        : this(protocolType: protocolType, host: IPAddress.Loopback.ToString(),
              port: portNumber, pfx: [], pfxPassword: string.Empty, reserved: [], clientAuthenticationEnabled: false)
    {
    }
}
