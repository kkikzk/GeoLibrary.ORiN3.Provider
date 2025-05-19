using Amazon.S3;
using Amazon.S3.Model;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Interface;

internal class AmazonS3ClientEx(string accessKey, string secretAccessKey, AmazonS3ConfigEx config) : IAmazonS3ClientEx
{
    private class MethodReverter(Func<string, string, AmazonS3ConfigEx, IAmazonS3ClientEx> createMethod) : IDisposable
    {
        private readonly Func<string, string, AmazonS3ConfigEx, IAmazonS3ClientEx> _createMethod = createMethod;

        public void Dispose()
        {
            CreateMethod = _createMethod;
        }
    }

    private class AmazonS3ClientExWrapper(AmazonS3Client client) : IAmazonS3ClientEx
    {
        private AmazonS3Client _client = client;

        public Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken token)
        {
            return _client.DeleteObjectAsync(request, token);
        }

        public Task<GetObjectMetadataResponse> GetObjectMetadataAsync(string bucketName, string key, CancellationToken token)
        {
            return _client.GetObjectMetadataAsync(bucketName, key, token);
        }

        public Task<ListObjectsV2Response> ListObjectsV2Async(ListObjectsV2Request request, CancellationToken token)
        {
            return _client.ListObjectsV2Async(request, token);
        }

        public Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken token)
        {
            return _client.PutObjectAsync(request, token);
        }

        public void Dispose()
        {
            _client.Dispose();
            _client = null!;
        }
    }

    private IAmazonS3ClientEx _client = CreateMethod(accessKey, secretAccessKey, config);

    private static Func<string, string, AmazonS3ConfigEx, IAmazonS3ClientEx> CreateMethod { get; set; } = (accessKey, secretAccessKey, config) =>
    {
        return new AmazonS3ClientExWrapper(new AmazonS3Client(accessKey, secretAccessKey, config));
    };

    public static IDisposable SetCreateMethod(Func<string, string, AmazonS3ConfigEx, IAmazonS3ClientEx> createMethod)
    {
        var methodReverterex = new MethodReverter(CreateMethod);
        CreateMethod = createMethod;
        return methodReverterex;
    }

    public Task<GetObjectMetadataResponse> GetObjectMetadataAsync(string bucketName, string key, CancellationToken token)
    {
        return _client.GetObjectMetadataAsync(bucketName, key, token);
    }

    public Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken token)
    {
        return _client.PutObjectAsync(request, token);
    }

    public Task<ListObjectsV2Response> ListObjectsV2Async(ListObjectsV2Request request, CancellationToken token)
    {
        return _client.ListObjectsV2Async(request, token);
    }

    public Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken token)
    {
        return _client.DeleteObjectAsync(request, token);
    }

    public void Dispose()
    {
        _client.Dispose();
        _client = null!;
    }
}
