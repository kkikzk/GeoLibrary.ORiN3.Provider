using Azure;
using Azure.Storage.Blobs.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;
using System;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class BlobContainerClientMock(string connectionString, string containerName) : IBlobContainerClient
{
    public string ConnectionString { get; } = connectionString;
    public string ContainerName { get; } = containerName;

    public Func<CancellationToken, Task<Response<BlobContainerInfo>>>? CreateIfNotExistsAsyncMock { get; set; }
    public Func<string, IBlobClient>? GetBlobClientMock { get; set; }
    public Func<string?, CancellationToken, IAsyncPageable<BlobItem>>? GetBlobsAsyncMock { get; set; }
    public Func<string, IAppendBlobClient>? GetAppendBlobClientMock { get; set; }
    public Func<string, IBlockBlobClient>? GetBlockBlobClientMock { get; set; }
    public Func<string, IPageBlobClient>? GetPageBlobClientMock { get; set; }

    public Task<Response<BlobContainerInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        if (CreateIfNotExistsAsyncMock != null)
        {
            return CreateIfNotExistsAsyncMock(token);
        }

        var info = BlobsModelFactory.BlobContainerInfo(
            lastModified: new DateTimeOffset(DateTime.Now),
            eTag: new ETag(Guid.NewGuid().ToString())
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }

    public IBlobClient GetBlobClient(string blobName)
    {
        if (GetBlobClientMock != null)
        {
            return GetBlobClientMock(blobName);
        }
        return new BlobClientMock(blobName);
    }

    public IAsyncPageable<BlobItem> GetBlobsAsync(string? prefix, CancellationToken cancellationToken)
    {
        if (GetBlobsAsyncMock != null)
        {
            return GetBlobsAsyncMock(prefix, cancellationToken);
        }
        return new AsyncPageableBlobItemMock();
    }

    public IAppendBlobClient GetAppendBlobClient(string blobName)
    {
        if (GetAppendBlobClientMock != null)
        {
            return GetAppendBlobClientMock(blobName);
        }
        return new AppendBlobClientMock(blobName);
    }

    public IBlockBlobClient GetBlockBlobClient(string blobPath)
    {
        if (GetBlockBlobClientMock != null)
        {
            return GetBlockBlobClientMock(blobPath);
        }
        return new BlockBlobClientMock(blobPath);
    }

    public IPageBlobClient GetPageBlobClient(string blobPath)
    {
        if (GetPageBlobClientMock != null)
        {
            return GetPageBlobClientMock(blobPath);
        }
        return new PageBlobClientMock(blobPath);
    }
}

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
}

internal class BlockBlobClientMock(string name, bool exists = true) : BlobBaseClientMock(name, exists), IBlockBlobClient
{
}

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

internal class AppendBlobClientMock(string name, bool exists = true) : BlobBaseClientMock(name, exists), IAppendBlobClient
{
    public Func<CancellationToken, Task<Response<BlobContentInfo>>>? CreateIfNotExistsAsyncMock { get; set; }
    public Func<Stream, AppendBlobAppendBlockOptions, CancellationToken, Task<Response<BlobAppendInfo>>>? AppendBlockAsyncMock { get; set; }

    public Task<Response<BlobContentInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        if (CreateIfNotExistsAsyncMock != null)
        {
            return CreateIfNotExistsAsyncMock(token);
        }

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

    public Task<Response<BlobAppendInfo>> AppendBlockAsync(Stream content, AppendBlobAppendBlockOptions options, CancellationToken token)
    {
        if (AppendBlockAsyncMock != null)
        {
            return AppendBlockAsyncMock(content, options, token);
        }

        var info = BlobsModelFactory.BlobAppendInfo(
            lastModified: new DateTimeOffset(DateTime.Now),
            eTag: new ETag(Guid.NewGuid().ToString()),
            contentHash: [],
            contentCrc64: [],
            blobAppendOffset: string.Empty,
            blobCommittedBlockCount: 0,
            isServerEncrypted: false,
            encryptionKeySha256: string.Empty,
            encryptionScope: string.Empty
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }
}
