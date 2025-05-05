using Azure;
using Azure.Storage.Files.Shares.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class ShareDirectoryClientMock(string name) : IShareDirectoryClient
{
    public string Name { private set; get; } = name;

    public Func<CancellationToken, Task<Response<ShareDirectoryInfo>>>? CreateIfNotExistsAsyncMock { get; set; }
    public Func<string, IShareFileClient>? GetFileClientMock { get; set; }
    public Func<string, IShareDirectoryClient>? GetSubdirectoryClientMock { get; set; }
    public Func<CancellationToken, IAsyncPageable<ShareFileItem>>? GetFilesAndDirectoriesAsyncMock { get; set; }

    public Task<Response<ShareDirectoryInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        if (CreateIfNotExistsAsyncMock != null)
        {
            return CreateIfNotExistsAsyncMock(token);
        }

        var info = FilesModelFactory.StorageDirectoryInfo(
            lastModified: new DateTimeOffset(DateTime.Now),
            eTag: new ETag(Guid.NewGuid().ToString())
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }

    public Task<Response<bool>> DeleteIfExistsAsync(CancellationToken token)
    {
        return Task.FromResult(Response.FromValue(false, null!));
    }

    public IShareFileClient GetFileClient(string fileName)
    {
        if (GetFileClientMock != null)
        {
            return GetFileClientMock(fileName);
        }

        return new ShareFileClientMock(fileName);
    }

    public IAsyncPageable<ShareFileItem> GetFilesAndDirectoriesAsync(CancellationToken token)
    {
        if (GetFilesAndDirectoriesAsyncMock != null)
        {
            return GetFilesAndDirectoriesAsyncMock(token);
        }

        return new AsyncPageableShareFileItemMock();
    }

    public IShareDirectoryClient GetSubdirectoryClient(string subdirectoryName)
    {
        if (GetSubdirectoryClientMock != null)
        {
            return GetSubdirectoryClientMock(subdirectoryName);
        }

        return new ShareDirectoryClientMock(subdirectoryName);
    }
}
