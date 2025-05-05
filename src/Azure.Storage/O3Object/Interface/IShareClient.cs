using Azure;
using Azure.Storage.Files.Shares.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal interface IShareClient
{
    Task<Response<ShareInfo>> CreateIfNotExistsAsync(CancellationToken token);
    IShareDirectoryClient GetRootDirectoryClient();
    IShareLeaseClient GetShareLeaseClient(string leaseId = null!);
}
