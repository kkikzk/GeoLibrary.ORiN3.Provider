using Azure;
using Azure.Storage.Blobs.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal interface IBlobClient
{
    string Name { get; }
    Uri Uri { get; }
    Task<Response<BlobContentInfo>> UploadAsync(Stream stream, bool overwrite, CancellationToken token);
    Task<Response> DeleteAsync(DeleteSnapshotsOption snapshotsOption, BlobRequestConditions conditions, CancellationToken cancellationToken);
}
