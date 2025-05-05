using Azure;
using Azure.Storage.Files.Shares.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal interface IShareDirectoryClient
{
    Task<Response<ShareDirectoryInfo>> CreateIfNotExistsAsync(CancellationToken token);
    IShareDirectoryClient GetSubdirectoryClient(string subdirectoryName);
    IShareFileClient GetFileClient(string fileName);
    IAsyncPageable<ShareFileItem> GetFilesAndDirectoriesAsync(CancellationToken token);
    public Task<Response<bool>> DeleteIfExistsAsync(CancellationToken token);
}
