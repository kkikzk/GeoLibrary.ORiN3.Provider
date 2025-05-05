using Azure;
using Azure.Storage.Blobs.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

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
