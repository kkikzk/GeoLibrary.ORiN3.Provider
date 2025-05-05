using Azure;
using Azure.Storage.Blobs.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class BlobContainerClientMock(string connectionString, string containerName) : IBlobContainerClient
{
    public string ConnectionString { get; } = connectionString;
    public string ContainerName { get; } = containerName;

    public Func<CancellationToken, Task<Response<BlobContainerInfo>>>? CreateIfNotExistsAsyncMock { get; set; }
    public Func<string, IBlobClient>? GetBlobClientMock { get; set; }
    public Func<string?, CancellationToken, IAsyncPageable<BlobItem>>? GetBlobsAsyncMock { get; set; }
    public Func<string, IAppendBlobClient>? GetAppendBlobClientMock { get; set; }
    public Func<string, IBlockBlobClient>? GetBlockBlobClientMock { get; set; }
    public Func<string, IPageBlobClient>? GetPageBlobClientMock { get; set; }

    public Task<Response<BlobContainerInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        if (CreateIfNotExistsAsyncMock != null)
        {
            return CreateIfNotExistsAsyncMock(token);
        }

        var info = BlobsModelFactory.BlobContainerInfo(
            lastModified: new DateTimeOffset(DateTime.Now),
            eTag: new ETag(Guid.NewGuid().ToString())
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }

    public IBlobClient GetBlobClient(string blobName)
    {
        if (GetBlobClientMock != null)
        {
            return GetBlobClientMock(blobName);
        }
        return new BlobClientMock(blobName);
    }

    public IAsyncPageable<BlobItem> GetBlobsAsync(string? prefix, CancellationToken cancellationToken)
    {
        if (GetBlobsAsyncMock != null)
        {
            return GetBlobsAsyncMock(prefix, cancellationToken);
        }
        return new AsyncPageableBlobItemMock();
    }

    public IAppendBlobClient GetAppendBlobClient(string blobName)
    {
        if (GetAppendBlobClientMock != null)
        {
            return GetAppendBlobClientMock(blobName);
        }
        return new AppendBlobClientMock(blobName);
    }

    public IBlockBlobClient GetBlockBlobClient(string blobPath)
    {
        if (GetBlockBlobClientMock != null)
        {
            return GetBlockBlobClientMock(blobPath);
        }
        return new BlockBlobClientMock(blobPath);
    }

    public IPageBlobClient GetPageBlobClient(string blobPath)
    {
        if (GetPageBlobClientMock != null)
        {
            return GetPageBlobClientMock(blobPath);
        }
        return new PageBlobClientMock(blobPath);
    }
}