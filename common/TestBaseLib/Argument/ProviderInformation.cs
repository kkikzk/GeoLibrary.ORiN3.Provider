using Design.ORiN3.Provider.Core.V1;
using System.Diagnostics;

namespace GeoLibrary.ORiN3.Provider.TestBaseLib.Argument;

[DebuggerStepThrough]
public class ProviderInformation(ProviderWakeupResultType wakeupResult,
    string message, IProviderEndPoint[] endPoints, byte[] reserved, int maintenancePort) : IProviderInformation
{
    public ProviderWakeupResultType WakeupResult { set;  get; } = wakeupResult;
    public string Message { set; get; } = message;
    public IProviderEndPoint[] EndPoints { set; get; } = endPoints;
    public byte[] Reserved { set; get; } = reserved;
    public int MaintenancePort { set; get; } = maintenancePort;

    public ProviderInformation()
        : this(wakeupResult: ProviderWakeupResultType.Succeeded, message: string.Empty, endPoints: [], reserved: [], maintenancePort: 0)
    {
    }
}
