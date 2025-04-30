using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

internal class PageBlobClientWrapper(PageBlobClient client) : BlobBaseClientWrapper(client), IPageBlobClient
{
    private readonly PageBlobClient _pageBlobClient = client;

    public Task<Response<BlobContentInfo>> CreateAsync(long size, CancellationToken token)
    {
        return _pageBlobClient.CreateAsync(size, cancellationToken: token);
    }

    public Task<Response<BlobDownloadInfo>> DownloadAsync(HttpRange range, BlobRequestConditions conditions, CancellationToken token)
    {
        return _pageBlobClient.DownloadAsync(range, conditions, cancellationToken: token);
    }

    public Task<Response<PageInfo>> UploadPagesAsync(Stream content, long offset, PageBlobUploadPagesOptions options, CancellationToken token)
    {
        return _pageBlobClient.UploadPagesAsync(content, offset, options, cancellationToken: token);
    }
}
