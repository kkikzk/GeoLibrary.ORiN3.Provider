using Azure;
using Azure.Storage.Blobs.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal interface IPageBlobClient : IBlobBaseClient
{
    Task<Response<BlobContentInfo>> CreateAsync(long size, CancellationToken token);
    Task<Response<BlobDownloadInfo>> DownloadAsync(HttpRange range, BlobRequestConditions conditions, CancellationToken token);
    Task<Response<PageInfo>> UploadPagesAsync(Stream content, long offset, PageBlobUploadPagesOptions options, CancellationToken token);
}