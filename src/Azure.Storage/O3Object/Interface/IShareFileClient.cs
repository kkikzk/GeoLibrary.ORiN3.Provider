using Azure;
using Azure.Storage.Files.Shares.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal interface IShareFileClient
{
    public Uri Uri { get; }
    Task<Response<ShareFileProperties>> GetPropertiesAsync(ShareFileRequestConditions condition, CancellationToken token);
    Task<Response<ShareFileDownloadInfo>> DownloadAsync(ShareFileDownloadOptions options, CancellationToken token);
    Task<Response<ShareFileUploadInfo>> UploadRangeAsync(HttpRange range, Stream content, ShareFileUploadRangeOptions options, CancellationToken token);
    Task<Response<bool>> ExistsAsync(CancellationToken token);
    Task<Response> DeleteAsync(CancellationToken token);
    Task<Response<ShareFileInfo>> CreateAsync(long maxSize, ShareFileRequestConditions conditions, CancellationToken token);
}
