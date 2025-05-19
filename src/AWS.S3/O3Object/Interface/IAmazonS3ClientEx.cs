using Amazon.S3.Model;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Interface;

public interface IAmazonS3ClientEx : IDisposable
{
    Task<GetObjectMetadataResponse> GetObjectMetadataAsync(string bucketName, string key, CancellationToken token);
    Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken token);
    Task<ListObjectsV2Response> ListObjectsV2Async(ListObjectsV2Request request, CancellationToken token);
    Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken token);
}
