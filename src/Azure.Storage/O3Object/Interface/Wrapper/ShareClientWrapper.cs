using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares.Specialized;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

internal class ShareClientWrapper(ShareClient shareClient) : IShareClient
{
    private readonly ShareClient _shareClient = shareClient;

    public Task<Response<ShareInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        return _shareClient.CreateIfNotExistsAsync(cancellationToken: token);
    }

    public IShareDirectoryClient GetRootDirectoryClient()
    {
        return new ShareDirectoryClientWrapper(_shareClient.GetRootDirectoryClient());
    }

    public IShareLeaseClient GetShareLeaseClient(string leaseId = null!)
    {
        return new ShareLeaseClientWrapper(_shareClient.GetShareLeaseClient(leaseId));
    }
}
internal class ShareLeaseClientWrapper(ShareLeaseClient shareLeaseClient) : IShareLeaseClient
{
    private readonly ShareLeaseClient _shareLeaseClient = shareLeaseClient;

    public Task<Response<ShareFileLease>> AcquireAsync(TimeSpan duration, CancellationToken token)
    {
        return _shareLeaseClient.AcquireAsync(duration, token);
    }
}

