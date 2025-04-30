using Design.ORiN3.Provider.Core.V1.Telemetry;
using System.Diagnostics;

namespace GeoLibrary.ORiN3.Provider.TestBaseLib.Argument;

[DebuggerStepThrough]
public class TelemetryInformation(IDictionary<string, string> attributes, ITelemetryEndpoint[] endpoints) : ITelemetryInformation
{
    public IDictionary<string, string> Attributes { get; } = attributes;
    public ITelemetryEndpoint[] Endpoints { get; } = endpoints;

    public TelemetryInformation()
        : this(attributes: new Dictionary<string, string>(), endpoints: [])
    {
    }
}
