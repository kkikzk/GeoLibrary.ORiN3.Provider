using Azure;
using Azure.Storage.Blobs.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal interface IBlobBaseClient
{
    Task<Response<bool>> ExistsAsync(CancellationToken token);
    Task<Response<BlobProperties>> GetPropertiesAsync(CancellationToken token);
    Task<Response> DownloadToAsync(string path, CancellationToken token);
    Task SetHttpHeadersAsync(BlobHttpHeaders blobHttpHeaders, CancellationToken cancellationToken);
}
