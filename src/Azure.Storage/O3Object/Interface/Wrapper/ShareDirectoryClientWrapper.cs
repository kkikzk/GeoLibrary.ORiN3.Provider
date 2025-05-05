using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

internal class ShareDirectoryClientWrapper(ShareDirectoryClient client) : IShareDirectoryClient
{
    private readonly ShareDirectoryClient _client = client;

    public Task<Response<ShareDirectoryInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        return _client.CreateIfNotExistsAsync(cancellationToken: token);
    }

    public Task<Response<bool>> DeleteIfExistsAsync(CancellationToken token)
    {
        return _client.DeleteIfExistsAsync(token);
    }

    public IShareFileClient GetFileClient(string fileName)
    {
        return new ShareFileClientWrapper(_client.GetFileClient(fileName));
    }

    public AsyncPageable<ShareFileItem> GetFilesAndDirectoriesAsync(CancellationToken token)
    {
        return _client.GetFilesAndDirectoriesAsync(cancellationToken: token);
    }

    public IShareDirectoryClient GetSubdirectoryClient(string subdirectoryName)
    {
        return new ShareDirectoryClientWrapper(_client.GetSubdirectoryClient(subdirectoryName));
    }

    IAsyncPageable<ShareFileItem> IShareDirectoryClient.GetFilesAndDirectoriesAsync(CancellationToken token)
    {
        var azurePageable = _client.GetFilesAndDirectoriesAsync(cancellationToken: token);
        return new AsyncPageableWrapper<ShareFileItem>(azurePageable);
    }
}
