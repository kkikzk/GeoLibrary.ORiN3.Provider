using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

internal class BlobBaseClientWrapper(BlobBaseClient client) : IBlobBaseClient
{
    private readonly BlobBaseClient _client = client;

    public Task<Response<bool>> ExistsAsync(CancellationToken token)
    {
        return _client.ExistsAsync(token);
    }

    public Task<Response<BlobProperties>> GetPropertiesAsync(CancellationToken token)
    {
        return _client.GetPropertiesAsync(cancellationToken: token);
    }

    public Task<Response> DownloadToAsync(string path, CancellationToken token)
    {
        return _client.DownloadToAsync(path, token);
    }
}
