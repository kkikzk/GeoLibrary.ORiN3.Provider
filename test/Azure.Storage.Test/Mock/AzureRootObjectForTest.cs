using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Root;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class AzureRootObjectForTest : AzureRootObject
{
    static AzureRootObjectForTest()
    {
        AuthorityCheckEnabled = false;
        LicenseCheckEnabled = false;
    }
}
