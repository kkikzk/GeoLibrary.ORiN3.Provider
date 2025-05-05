using Azure;
using Azure.Storage.Files.Shares.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class ShareClientMock(string connectionString, string shareName) : IShareClient
{
    public string ConnectionString { get; } = connectionString;
    public string ShareName { get; } = shareName;

    public Func<CancellationToken, Task<Response<ShareInfo>>>? CreateIfNotExistsAsyncMock { get; set; }
    public Func<IShareDirectoryClient>? GetRootDirectoryClientMock { get; set; }
    public Func<string, IShareLeaseClient>? GetShareLeaseClientMock { get; set; }

    public Task<Response<ShareInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        if (CreateIfNotExistsAsyncMock != null)
        {
            return CreateIfNotExistsAsyncMock(token);
        }

        var info = ShareModelFactory.ShareInfo(
            lastModified: new DateTimeOffset(DateTime.Now),
            eTag: new ETag(Guid.NewGuid().ToString())
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }

    public IShareDirectoryClient GetRootDirectoryClient()
    {
        if (GetRootDirectoryClientMock != null)
        {
            return GetRootDirectoryClientMock();
        }

        return new ShareDirectoryClientMock(string.Empty);
    }

    public IShareLeaseClient GetShareLeaseClient(string leaseId = null!)
    {
        if (GetShareLeaseClientMock != null)
        {
            return GetShareLeaseClientMock(leaseId);
        }

        return new ShareLeaseClientMock(leaseId);
    }
}
