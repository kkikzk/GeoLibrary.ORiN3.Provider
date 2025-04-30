using Azure;
using Azure.Core;
using Azure.Storage.Blobs.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

public class MockResponse : Response
{
    public override int Status => 200;
    public override string ReasonPhrase => "OK";
    public override Stream? ContentStream { get; set; }
    public override string ClientRequestId { get; set; } = string.Empty;
    public override void Dispose() { GC.SuppressFinalize(this); }
    protected override bool ContainsHeader(string name) => false;
    protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];
    protected override bool TryGetHeader(string name, out string value) => throw new NotImplementedException();
    protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values) => throw new NotImplementedException();
}

internal class BlobClientMock(string blobName) : IBlobClient
{
    public string Name { private set; get; } = blobName;
    public Uri Uri { set; get; } = new Uri("http://hoge.com");

    public Func<Stream, bool, CancellationToken, Task<Response<BlobContentInfo>>>? UploadAsyncMock { get; set; }
    public Func<DeleteSnapshotsOption, BlobRequestConditions, CancellationToken, Task<Response>>? DeleteAsyncMock { get; set; }

    public Task<Response> DeleteAsync(DeleteSnapshotsOption snapshotsOption, BlobRequestConditions conditions, CancellationToken cancellationToken)
    {
        if (DeleteAsyncMock != null)
        {
            return DeleteAsyncMock(snapshotsOption, conditions, cancellationToken);
        }

        return Task.FromResult((Response)new MockResponse());
    }

    public Task<Response<BlobContentInfo>> UploadAsync(Stream stream, bool overwrite, CancellationToken token)
    {
        if (UploadAsyncMock != null)
        {
            return UploadAsyncMock(stream, overwrite, token);
        }

        var info = BlobsModelFactory.BlobContentInfo(
            eTag: new ETag(Guid.NewGuid().ToString()),
            lastModified: new DateTimeOffset(DateTime.Now),
            contentHash: [],
            versionId: string.Empty,
            encryptionKeySha256: string.Empty,
            encryptionScope: string.Empty,
            blobSequenceNumber: 0
        );
        return Task.FromResult(Response.FromValue(info, null!));
    }
}
