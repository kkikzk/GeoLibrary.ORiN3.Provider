using Azure;
using Azure.Storage.Blobs.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class BlobBaseClientMock(string name, bool exists) : IBlobBaseClient
{
    private readonly bool _exists = exists;

    public string Name => name;
    public Uri Uri => new($"http://hoge.com/{name}");

    public Func<CancellationToken, Task<Response<bool>>>? ExistsAsyncMock { get; set; }
    public Func<CancellationToken, Task<Response<BlobProperties>>>? GetPropertiesAsyncMock { get; set; }
    public Func<string, CancellationToken, Task<Response>>? DownloadToAsyncMock { get; set; }

    public Task<Response<bool>> ExistsAsync(CancellationToken token)
    {
        if (ExistsAsyncMock != null)
        {
            return ExistsAsyncMock(token);
        }
        return Task.FromResult(Response.FromValue(_exists, null!));
    }

    public Task<Response<BlobProperties>> GetPropertiesAsync(CancellationToken token)
    {
        if (GetPropertiesAsyncMock != null)
        {
            return GetPropertiesAsyncMock(token);
        }

        var info = BlobsModelFactory.BlobProperties();
        return Task.FromResult(Response.FromValue(info, null!));
    }

    public Task<Response> DownloadToAsync(string path, CancellationToken token)
    {
        if (DownloadToAsyncMock != null)
        {
            return DownloadToAsyncMock(path, token);
        }

        using var stream = File.OpenWrite(path);
        stream.Write([1, 2, 3]);
        return Task.FromResult((Response)new MockResponse());
    }

    public Task SetHttpHeadersAsync(BlobHttpHeaders blobHttpHeaders, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
