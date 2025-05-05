using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

internal class AppendBlobClientWrapper(AppendBlobClient client) : IAppendBlobClient
{
    private readonly AppendBlobClient _client = client;

    public string Name => _client.Name;

    public Uri Uri => _client.Uri;

    public Task<Response<BlobContentInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        return _client.CreateIfNotExistsAsync(cancellationToken: token);
    }

    public Task<Response<BlobAppendInfo>> AppendBlockAsync(Stream content, AppendBlobAppendBlockOptions options, CancellationToken token)
    {
        return _client.AppendBlockAsync(content, options, token);
    }

    public Task<Response<bool>> ExistsAsync(CancellationToken token)
    {
        return _client.ExistsAsync(cancellationToken: token);
    }

    public Task<Response<BlobProperties>> GetPropertiesAsync(CancellationToken token)
    {
        return _client.GetPropertiesAsync(cancellationToken: token);
    }

    public Task<Response> DownloadToAsync(string path, CancellationToken token)
    {
        return _client.DownloadToAsync(path, cancellationToken: token);
    }

    public Task SetHttpHeadersAsync(BlobHttpHeaders blobHttpHeaders, CancellationToken cancellationToken)
    {
        return _client.SetHttpHeadersAsync(blobHttpHeaders, cancellationToken: cancellationToken);
    }
}
