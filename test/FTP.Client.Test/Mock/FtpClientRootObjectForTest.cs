using GeoLibrary.ORiN3.Provider.FTP.Client.O3Object.Root;

namespace GeoLibrary.ORiN3.Provider.FTP.Client.Test.Mock;

internal class FtpClientRootObjectForTest : FtpClientRootObject
{
    static FtpClientRootObjectForTest()
    {
        AuthorityCheckEnabled = false;
        LicenseCheckEnabled = false;
    }
}
