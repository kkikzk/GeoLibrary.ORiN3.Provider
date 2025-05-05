using Azure;
using Azure.Storage.Files.Shares.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;
using System.Reflection;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class ShareFileClientMock(string name) : IShareFileClient
{
    private readonly string _name = name;

    public byte[] Buffer { get; set; } = [];
    public string Name { get { return _name; } }
    public Uri Uri => new($"http://hoge.com/{_name}");

    public Func<long, ShareFileRequestConditions, CancellationToken, Task<Response<ShareFileInfo>>>? CreateAsyncMock { get; set; }
    public Func<ShareFileRequestConditions, CancellationToken, Task<Response<ShareFileProperties>>>? GetPropertiesAsyncMock { get; set; }
    public Func<CancellationToken,　Task<Response<bool>>>? ExistsAsyncMock { get; set; }
    public Func<ShareFileDownloadOptions, CancellationToken, Task<Response<ShareFileDownloadInfo>>>? DownloadAsyncMock { get; set; }
    public Func<HttpRange, Stream, ShareFileUploadRangeOptions, CancellationToken, Task<Response<ShareFileUploadInfo>>>? UploadPagesAsyncMock { get; set; }

    public Task<Response<ShareFileInfo>> CreateAsync(long maxSize, ShareFileRequestConditions conditions, CancellationToken token)
    {
        if (CreateAsyncMock != null)
        {
            return CreateAsyncMock(maxSize, conditions, token);
        }
        Buffer = new byte[maxSize];

        var now = DateTime.Now;
        var info = FilesModelFactory.StorageFileInfo(
            lastModified: new DateTimeOffset(now),
            eTag: new ETag(Guid.NewGuid().ToString()),
            isServerEncrypted: false,
            fileCreationTime: new DateTimeOffset(now),
            fileLastWriteTime: new DateTimeOffset(now),
            fileChangeTime: new DateTimeOffset(now)
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }

    public Task<Response> DeleteAsync(CancellationToken token)
    {
        return Task.FromResult((Response)new MockResponse());
    }

    public Task<Response<ShareFileDownloadInfo>> DownloadAsync(ShareFileDownloadOptions options, CancellationToken token)
    {
        if (DownloadAsyncMock != null)
        {
            return DownloadAsyncMock(options, token);
        }
        var info = FilesModelFactory.StorageFileDownloadInfo(
            content: new MemoryStream(Buffer, (int)options.Range.Offset, (int)options.Range.Length!)
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }

    public Task<Response<bool>> ExistsAsync(CancellationToken token)
    {
        if (ExistsAsyncMock != null)
        {
            return ExistsAsyncMock(token);
        }

        return Task.FromResult(Response.FromValue(true, null!));
    }

    public Task<Response<ShareFileProperties>> GetPropertiesAsync(ShareFileRequestConditions condition, CancellationToken token)
    {
        if (GetPropertiesAsyncMock != null)
        {
            return GetPropertiesAsyncMock(condition, token);
        }

        var info = FilesModelFactory.StorageFileProperties(
            contentLength: Buffer.LongLength
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }

    public async Task<Response<ShareFileUploadInfo>> UploadRangeAsync(HttpRange range, Stream content, ShareFileUploadRangeOptions options, CancellationToken token)
    {
        if (UploadPagesAsyncMock != null)
        {
            return await UploadPagesAsyncMock(range, content, options, token).ConfigureAwait(false);
        }
        await content.ReadAsync(Buffer.AsMemory().Slice((int)range.Offset, (int)range.Length!), token).ConfigureAwait(false);

        var ctor = typeof(ShareFileUploadInfo).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);
        var info = (ShareFileUploadInfo)ctor!.Invoke(null);
        return Response.FromValue(info, null!);
    }
}