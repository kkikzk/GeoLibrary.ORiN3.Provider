using Azure;
using Azure.Storage.Blobs.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal interface IAppendBlobClient : IBlobBaseClient
{
    string Name { get; }
    Uri Uri { get; }
    Task<Response<BlobContentInfo>> CreateIfNotExistsAsync(CancellationToken token);
    Task<Response<BlobAppendInfo>> AppendBlockAsync(Stream content, AppendBlobAppendBlockOptions options, CancellationToken token);
}
