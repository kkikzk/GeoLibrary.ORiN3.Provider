using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

internal class BlobClientWrapper(BlobClient client) : IBlobClient
{
    private readonly BlobClient _client = client;

    public string Name => _client.Name;
    public Uri Uri => _client.Uri;

    public Task<Response<BlobContentInfo>> UploadAsync(Stream stream, bool overwrite, CancellationToken token)
    {
        return _client.UploadAsync(stream, overwrite, token);
    }

    public Task<Response> DeleteAsync(DeleteSnapshotsOption snapshotsOption, BlobRequestConditions conditions, CancellationToken cancellationToken)
    {
        return _client.DeleteAsync(snapshotsOption, conditions, cancellationToken);
    }
}
