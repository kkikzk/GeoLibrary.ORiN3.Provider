using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal class BlobContainerClientEx(string connectionString, string containerName) : IBlobContainerClient
{
    private class MethodReverter(Func<string, string, IBlobContainerClient> createMethod) : IDisposable
    {
        private readonly Func<string, string, IBlobContainerClient> _createMethod = createMethod;

        public void Dispose()
        {
            CreateMethod = _createMethod;
        }
    }

    private readonly IBlobContainerClient _client = CreateMethod(connectionString, containerName);

    private static Func<string, string, IBlobContainerClient> CreateMethod { get; set; } = (connectionString, containeName) => new BlobContainerClientWrapper(new BlobContainerClient(connectionString, containeName));

    public static IDisposable SetCreateMethod(Func<string, string, IBlobContainerClient> createMethod)
    {
        var methodReverterex = new MethodReverter(CreateMethod);
        CreateMethod = createMethod;
        return methodReverterex;
    }

    public Task<Response<BlobContainerInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        return _client.CreateIfNotExistsAsync(token);
    }

    public virtual IBlobClient GetBlobClient(string blobName)
    {
        return _client.GetBlobClient(blobName);
    }

    public IAsyncPageable<BlobItem> GetBlobsAsync(string? prefix, CancellationToken cancellationToken)
    {
        return _client.GetBlobsAsync(prefix, cancellationToken);
    }

    public IAppendBlobClient GetAppendBlobClient(string blobName)
    {
        return _client.GetAppendBlobClient(blobName);
    }

    public IBlockBlobClient GetBlockBlobClient(string blobPath)
    {
        return _client.GetBlockBlobClient(blobPath);
    }

    public IPageBlobClient GetPageBlobClient(string blobPath)
    {
        return _client.GetPageBlobClient(blobPath);
    }
}
