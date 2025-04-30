using Design.ORiN3.Provider.Core.V1;
using Design.ORiN3.Provider.Core.V1.Telemetry;
using System.Diagnostics;
using System.Reflection;

namespace GeoLibrary.ORiN3.Provider.TestBaseLib.Argument;

[DebuggerStepThrough]
public class ProviderCoreStartupArgument(string userDefinedId, string startupFinishedResponseEventName, bool threadSafeMode,
    IProviderCoreStartupArgumentEndpointInfo[] endpoints, IStartupHandshakeInformation startupHandshakeInformation, string pipeName,
    ORiN3LogLevel logLevel, ITelemetryInformation telemetryInformation, IDictionary<string, string> extension, string configFile) : IProviderCoreStartupArgument
{
    public string UserDefinedId { get; } = userDefinedId;
    public string StartupFinishedResponseEventName { get; } = startupFinishedResponseEventName;
    public bool ThreadSafeMode { get; } = threadSafeMode;
    public IProviderCoreStartupArgumentEndpointInfo[] Endpoints { get; } = endpoints;
    public IStartupHandshakeInformation StartupHandshakeInformation { get; } = startupHandshakeInformation;
    public string PipeName { get; } = pipeName;
    public ORiN3LogLevel LogLevel { get; } = logLevel;
    public ITelemetryInformation TelemetryInformation { get; } = telemetryInformation;
    public IDictionary<string, string> Extension { get; } = extension;
    public string ConfigFile { get; } = configFile;

    public ProviderCoreStartupArgument(ProviderCoreStartupArgumentEndpointInfo endpointInfo)
        : this(userDefinedId: Guid.NewGuid().ToString(), startupFinishedResponseEventName: Guid.NewGuid().ToString(),
              threadSafeMode: true, endpoints: [endpointInfo], startupHandshakeInformation: new StartupHandshakeInformation(),
              pipeName: Guid.NewGuid().ToString(), logLevel: ORiN3LogLevel.Trace, telemetryInformation: new TelemetryInformation(),
              extension: new Dictionary<string, string>(), configFile: Path.Combine(new FileInfo(Assembly.GetEntryAssembly()!.Location ?? throw new Exception()).Directory?.FullName ?? string.Empty, ".orin3providerconfig"))
    {
    }
}
