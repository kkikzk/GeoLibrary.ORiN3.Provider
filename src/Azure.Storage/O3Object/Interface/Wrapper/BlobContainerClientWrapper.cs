using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

internal class BlobContainerClientWrapper(BlobContainerClient client) : IBlobContainerClient
{
    private readonly BlobContainerClient _client = client;

    public Task<Response<BlobContainerInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        return _client.CreateIfNotExistsAsync(cancellationToken: token);
    }

    public virtual IBlobClient GetBlobClient(string blobName)
    {
        return new BlobClientWrapper(_client.GetBlobClient(blobName));
    }

    public IAsyncPageable<BlobItem> GetBlobsAsync(string? prefix, CancellationToken cancellationToken)
    {

        var azurePageable = _client.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken);
        return new AsyncPageableWrapper<BlobItem>(azurePageable);
    }

    public IAppendBlobClient GetAppendBlobClient(string blobName)
    {
        return new AppendBlobClientWrapper(_client.GetAppendBlobClient(blobName));
    }

    public IBlockBlobClient GetBlockBlobClient(string blobPath)
    {
        return new BlockBlobClientWrapper(_client.GetBlockBlobClient(blobPath));
    }

    public IPageBlobClient GetPageBlobClient(string blobPath)
    {
        throw new NotImplementedException();
    }
}
