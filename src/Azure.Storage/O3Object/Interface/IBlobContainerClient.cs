using Azure;
using Azure.Storage.Blobs.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal interface IBlobContainerClient
{
    Task<Response<BlobContainerInfo>> CreateIfNotExistsAsync(CancellationToken token);
    IBlobClient GetBlobClient(string blobName);
    IAsyncPageable<BlobItem> GetBlobsAsync(string? prefix, CancellationToken cancellationToken);
    IAppendBlobClient GetAppendBlobClient(string blobName);
    IBlockBlobClient GetBlockBlobClient(string blobPath);
    IPageBlobClient GetPageBlobClient(string blobPath);
}
