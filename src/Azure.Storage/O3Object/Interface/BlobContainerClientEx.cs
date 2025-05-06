using Azure;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;
using System.Net;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal class BlobContainerClientEx(string connectionString, string proxyUri, string containerName) : IBlobContainerClient
{
    private class MethodReverter(Func<string, string, string, IBlobContainerClient> createMethod) : IDisposable
    {
        private readonly Func<string, string, string, IBlobContainerClient> _createMethod = createMethod;

        public void Dispose()
        {
            CreateMethod = _createMethod;
        }
    }

    private readonly IBlobContainerClient _client = CreateMethod(connectionString, proxyUri, containerName);

    private static Func<string, string, string, IBlobContainerClient> CreateMethod { get; set; } = (connectionString, proxyUri, containeName) =>
    {
        if (string.IsNullOrEmpty(proxyUri))
        {
            return new BlobContainerClientWrapper(new BlobContainerClient(connectionString, containeName));
        }
        else
        {
            var proxy = new WebProxy(proxyUri);
            var handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true
            };
            var options = new BlobClientOptions
            {
                Transport = new HttpClientTransport(handler)
            };
            return new BlobContainerClientWrapper(new BlobContainerClient(connectionString, containeName, options));
        }
    };

    public static IDisposable SetCreateMethod(Func<string, string, string, IBlobContainerClient> createMethod)
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
