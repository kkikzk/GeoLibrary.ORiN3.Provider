using Amazon.S3.Model;
using GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.Test.Mock;

internal class AmazonS3ClientMock(string accessKey, string secretAccessKey, AmazonS3ConfigEx config) : IAmazonS3ClientEx
{
    private readonly string _accessKey = accessKey;
    private readonly string _secretAccessKey = secretAccessKey;
    private readonly AmazonS3ConfigEx _config = config;

    public Func<string, string, CancellationToken, Task<GetObjectMetadataResponse>>? GetObjectMetadataAsyncMock { get; set; }
    public Func<PutObjectRequest, CancellationToken, Task<PutObjectResponse>>? PutObjectAsyncMock { get; set; }
    public Func<ListObjectsV2Request, CancellationToken, Task<ListObjectsV2Response>>? ListObjectsV2AsyncMock { get; set; }
    public Func<DeleteObjectRequest, CancellationToken, Task<DeleteObjectResponse>>? DeleteObjectAsyncMock { get; set; }

    public Task<GetObjectMetadataResponse> GetObjectMetadataAsync(string bucketName, string key, CancellationToken token)
    {
        if (GetObjectMetadataAsyncMock != null)
        {
            return GetObjectMetadataAsyncMock(bucketName, key, token);
        }
        return Task.FromResult(new GetObjectMetadataResponse());
    }

    public Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken token)
    {
        if (PutObjectAsyncMock != null)
        {
            return PutObjectAsyncMock(request, token);
        }
        return Task.FromResult(new PutObjectResponse());
    }

    public Task<ListObjectsV2Response> ListObjectsV2Async(ListObjectsV2Request request, CancellationToken token)
    {
        if (ListObjectsV2AsyncMock != null)
        {
            return ListObjectsV2AsyncMock(request, token);
        }
        return Task.FromResult(new ListObjectsV2Response());
    }

    public Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken token)
    {
        if (DeleteObjectAsyncMock != null)
        {
            return DeleteObjectAsyncMock(request, token);
        }
        return Task.FromResult(new DeleteObjectResponse());
    }

    public void Dispose()
    {
    }
}
