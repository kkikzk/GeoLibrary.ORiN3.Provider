using Design.ORiN3.Provider.Core.V1;
using System.Diagnostics;

namespace GeoLibrary.ORiN3.Provider.TestBaseLib.Argument;

[DebuggerStepThrough]
public class ProviderEndPoint(int index, string uri, int protocolType) : IProviderEndPoint
{
    public int Index { get; set; } = index;
    public string Uri { get; set; } = uri;
    public int ProtocolType { get; set; } = protocolType;

    public ProviderEndPoint()
        : this(index: 0, uri: string.Empty, protocolType: 0)
    {
    }
}
