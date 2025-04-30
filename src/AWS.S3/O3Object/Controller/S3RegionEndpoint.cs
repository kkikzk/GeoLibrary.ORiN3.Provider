using ORiN3.Provider.Core.Abstract;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller;

internal class S3RegionEndpoint : ControllerBase
{
    private class S3RegionEndpointOption
    {

    }

    protected override Task OnInitializingAsync(JsonElement option, bool needVersionCheck, object? fromParent, CancellationToken token)
    {
        return base.OnInitializingAsync(option, needVersionCheck, fromParent, token);
    }
}
