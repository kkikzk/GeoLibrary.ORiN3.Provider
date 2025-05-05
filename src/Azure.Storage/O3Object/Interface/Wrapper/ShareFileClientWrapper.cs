using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

internal class ShareFileClientWrapper(ShareFileClient client) : IShareFileClient
{
    private readonly ShareFileClient _client = client;

    public Uri Uri => _client.Uri;

    public Task<Response<ShareFileInfo>> CreateAsync(long maxSize, ShareFileRequestConditions conditions, CancellationToken token)
    {
        return _client.CreateAsync(maxSize: maxSize, options: null, conditions: conditions, token);
    }

    public Task<Response> DeleteAsync(CancellationToken token)
    {
        return _client.DeleteAsync(token);
    }

    public Task<Response<ShareFileDownloadInfo>> DownloadAsync(ShareFileDownloadOptions options, CancellationToken token)
    {
        return _client.DownloadAsync(options, token);
    }

    public Task<Response<bool>> ExistsAsync(CancellationToken token)
    {
        return _client.ExistsAsync(token);
    }

    public Task<Response<ShareFileProperties>> GetPropertiesAsync(ShareFileRequestConditions condition, CancellationToken token)
    {
        return _client.GetPropertiesAsync(condition, token);
    }

    public Task<Response<ShareFileUploadInfo>> UploadRangeAsync(HttpRange range, Stream content, ShareFileUploadRangeOptions options, CancellationToken token)
    {
        return _client.UploadRangeAsync(range, content, options, token);
    }
}
