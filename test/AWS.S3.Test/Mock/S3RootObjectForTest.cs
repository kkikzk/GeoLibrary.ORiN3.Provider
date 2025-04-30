using GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Root;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.Test.Mock;

internal class S3RootObjectForTest : S3RootObject
{
    static S3RootObjectForTest()
    {
        AuthorityCheckEnabled = false;
        LicenseCheckEnabled = false;
    }
}
