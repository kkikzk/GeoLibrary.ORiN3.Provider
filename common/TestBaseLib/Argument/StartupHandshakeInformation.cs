using Design.ORiN3.Provider.Core.V1;
using System.Diagnostics;

namespace GeoLibrary.ORiN3.Provider.TestBaseLib.Argument;

[DebuggerStepThrough]
public class StartupHandshakeInformation(
    bool enableHandshake, string startupFinishedEventName, string startupFinishedResponseEventName) : IStartupHandshakeInformation
{
    public bool EnableHandshake { get; } = enableHandshake;
    public string StartupFinishedEventName { get; } = startupFinishedEventName;
    public string StartupFinishedResponseEventName { get; } = startupFinishedResponseEventName;

    public StartupHandshakeInformation()
        : this(enableHandshake: true, startupFinishedEventName: Guid.NewGuid().ToString(), startupFinishedResponseEventName: Guid.NewGuid().ToString())
    {
    }
}
