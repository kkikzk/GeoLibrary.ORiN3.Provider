using Azure;
using Azure.Storage.Blobs.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class PageBlobClientMock(string name, bool exists = true) : BlobBaseClientMock(name, exists), IPageBlobClient
{
    private byte[] _buffer = exists ? new byte[512] : [];

    public Func<long, CancellationToken, Task<Response<BlobContentInfo>>>? CreateAsyncMock { get; set; }
    public Func<HttpRange, BlobRequestConditions, CancellationToken, Task<Response<BlobDownloadInfo>>>? DownloadAsyncMock { get; set; }
    public Func<Stream, long, PageBlobUploadPagesOptions, CancellationToken, Task<Response<PageInfo>>>? UploadPagesAsyncMock { get; set; }

    public Task<Response<BlobContentInfo>> CreateAsync(long size, CancellationToken token)
    {
        if (CreateAsyncMock != null)
        {
            return CreateAsyncMock(size, token);
        }

        if (size % 512 != 0)
        {
            throw new RequestFailedException("Blob size must be a multiple of 512 bytes.");
        }
        _buffer = new byte[size];

        var info = BlobsModelFactory.BlobContentInfo(
            lastModified: new DateTimeOffset(DateTime.Now),
            eTag: new ETag(Guid.NewGuid().ToString()),
            contentHash: [],
            versionId: string.Empty,
            encryptionKeySha256: string.Empty,
            encryptionScope: string.Empty,
            blobSequenceNumber: 0
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }

    public Task<Response<BlobDownloadInfo>> DownloadAsync(HttpRange range, BlobRequestConditions conditions, CancellationToken token)
    {
        if (DownloadAsyncMock != null)
        {
            return DownloadAsyncMock(range, conditions, token);
        }
        var info = BlobsModelFactory.BlobDownloadInfo(
            content: new MemoryStream(_buffer, (int)range.Offset, (int)range.Length!)
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }

    public async Task<Response<PageInfo>> UploadPagesAsync(Stream content, long offset, PageBlobUploadPagesOptions options, CancellationToken token)
    {
        if (UploadPagesAsyncMock != null)
        {
            return await UploadPagesAsyncMock(content, offset, options, token).ConfigureAwait(false);
        }
        await content.ReadAsync(_buffer.AsMemory().Slice((int)offset, (int)content.Length), token).ConfigureAwait(false);

        var info = BlobsModelFactory.PageInfo(
            eTag: new ETag(Guid.NewGuid().ToString()),
            lastModified: new DateTimeOffset(DateTime.Now),
            contentHash: [],
            contentCrc64: [],
            blobSequenceNumber: 0,
            encryptionKeySha256: string.Empty,
            encryptionScope: string.Empty
        );
        return Response.FromValue(info, null!);
    }
}
