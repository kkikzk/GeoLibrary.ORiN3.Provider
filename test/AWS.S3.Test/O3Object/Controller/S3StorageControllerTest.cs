using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Colda.CommonUtilities.IO;
using GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller;
using GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Interface;
using GeoLibrary.ORiN3.Provider.AWS.S3.Test.Mock;
using GeoLibrary.ORiN3.Provider.AWS.S3.Test.TestUtilities;
using GeoLibrary.ORiN3.Provider.TestBaseLib;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.Test.O3Object.Controller;

public class S3StorageControllerTest(ProviderTestFixture<S3StorageControllerTest> fixture, ITestOutputHelper output) : TestBase<S3StorageControllerTest>(fixture, output)
{
    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObject")]
    public async Task UploadObjectTest()
    {
        var actualAccessKey = string.Empty;
        var actualSecretAccessKey = string.Empty;
        AmazonS3Config? actualConfig = null;
        var bucketName = "mytestawsbacket2025";
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            actualAccessKey = accessKey;
            actualSecretAccessKey = secretAccessKey;
            actualConfig = config;
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                GetObjectMetadataAsyncMock = (bucketName, key, token) =>
                {
                    throw new AmazonS3Exception("hoge", ErrorType.Unknown, "", "", HttpStatusCode.NotFound);
                },
                PutObjectAsyncMock = async (request, token) =>
                {
                    Assert.Equal(bucketName, request.BucketName);
                    Assert.Equal("hogehoge2.txt", request.Key);
                    request.InputStream.Seek(0, SeekOrigin.Begin);
                    Assert.Equal(5, request.InputStream.Length);
                    var buffer = new byte[5];
                    var readLength = await request.InputStream.ReadAsync(buffer, token);
                    Assert.Equal(5, readLength);
                    Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, buffer);
                    return new PutObjectResponse() { ETag = "aaa", HttpStatusCode = HttpStatusCode.OK };
                },
            };
        });
        using var cts = new CancellationTokenSource(100000000);
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
            { "Use Https", true },
            { "Proxy Uri", "http://dng-proxy-o.denso.co.jp:8080" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", bucketName },
            { "Bytes", new byte[] { 1, 2, 3, 4, 5 } },
            { "Object Key", "hogehoge2.txt" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObject", arguments, cts.Token);

        Assert.Equal(0, result["Result"]);
        Assert.Equal("aaa", result["ETag"]);
        Assert.Equal("hogehoge2.txt", result["Object Key"]);
        Assert.Equal("https://mytestawsbacket2025.s3.ap-northeast-1.amazonaws.com/hogehoge2.txt", result["Uri"]);
        Assert.Equal("DummyAccessKey", actualAccessKey);
        Assert.Equal("DummySecretAccessKey", actualSecretAccessKey);
        Assert.NotNull(actualConfig);
        Assert.Equal("ap-northeast-1", actualConfig.RegionEndpoint.SystemName);
        Assert.False(actualConfig.UseHttp);
        Assert.Equal(new Uri("http://dng-proxy-o.denso.co.jp:8080"), ((WebProxy)actualConfig.GetWebProxy()).Address);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObject")]
    public async Task UploadObjectErrorTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            //{ "Bucket Name", "testBucketName" },
            { "Bytes", new byte[] { 1, 2, 3, 4, 5 } },
            { "Object Key", "hogehoge2.txt" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObject", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("Bucket Name", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObject")]
    public async Task UploadObjectErrorTest2()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "testBucketName" },
            //{ "Bytes", new byte[] { 1, 2, 3, 4, 5 } },
            { "Object Key", "hogehoge2.txt" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObject", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("Bytes", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObject")]
    public async Task UploadObjectErrorTest3()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "testBucketName" },
            { "Bytes", new byte[] { 1, 2, 3, 4, 5 } },
            //{ "Object Key", "hogehoge2.txt" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObject", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("Object Key", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObject")]
    public async Task UploadObjectErrorTest4()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        var called = false;
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                GetObjectMetadataAsyncMock = (bucketName, key, token) =>
                {
                    called = true;
                    throw new AmazonS3Exception("hoge", ErrorType.Unknown, "", "", HttpStatusCode.NotFound);
                },
                PutObjectAsyncMock = (request, token) =>
                {
                    return Task.FromResult(new PutObjectResponse() { ETag = "aaa", HttpStatusCode = HttpStatusCode.OK });
                },
            };
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "testBucketName" },
            { "Bytes", new byte[] { 1, 2, 3, 4, 5 } },
            { "Object Key", "hogehoge2.txt" },
            //{ "Overwrite", false }, <= optional
        };
        var result = await controller.ExecuteAsync("UploadObject", arguments, cts.Token);

        // assert
        Assert.False(called);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObject")]
    public async Task UploadObjectErrorTest5()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        var called = false;
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                GetObjectMetadataAsyncMock = (bucketName, key, token) =>
                {
                    called = true;
                    return Task.FromResult(new GetObjectMetadataResponse());
                },
            };
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "testBucketName" },
            { "Bytes", new byte[] { 1, 2, 3, 4, 5 } },
            { "Object Key", "hogehoge2.txt" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObject", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.True(called);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromFile")]
    public async Task UploadObjectFromFileTest()
    {
        var fileInfo = TestDir.Get("UploadObjectFromFile").CombineWithFileName("abc.txt");
        var actualAccessKey = string.Empty;
        var actualSecretAccessKey = string.Empty;
        AmazonS3Config? actualConfig = null;
        var bucketName = "mytestawsbacket2025";
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            actualAccessKey = accessKey;
            actualSecretAccessKey = secretAccessKey;
            actualConfig = config;
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                GetObjectMetadataAsyncMock = (bucketName, key, token) =>
                {
                    throw new AmazonS3Exception("hoge", ErrorType.Unknown, "", "", HttpStatusCode.NotFound);
                },
                PutObjectAsyncMock = async (request, token) =>
                {
                    Assert.Equal(bucketName, request.BucketName);
                    Assert.Equal("hoge/abc.txt", request.Key);
                    request.InputStream.Seek(0, SeekOrigin.Begin);
                    Assert.Equal(3, request.InputStream.Length);
                    var buffer = new byte[3];
                    var readLength = await request.InputStream.ReadAsync(buffer, token);
                    Assert.Equal(3, readLength);
                    Assert.Equal("abc", Encoding.UTF8.GetString(buffer));
                    return new PutObjectResponse() { ETag = "aaa", HttpStatusCode = HttpStatusCode.OK };
                },
            };
        });
        using var cts = new CancellationTokenSource(100000000);
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
            { "Use Https", true },
            { "Proxy Uri", "http://dng-proxy-o.denso.co.jp:8080" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", bucketName },
            { "File Path", fileInfo.FullName },
            { "Prefix", "hoge" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObjectFromFile", arguments, cts.Token);

        Assert.Equal(0, result["Result"]);
        Assert.Equal("aaa", result["ETag"]);
        Assert.Equal("hoge/abc.txt", result["Object Key"]);
        Assert.Equal("https://mytestawsbacket2025.s3.ap-northeast-1.amazonaws.com/hoge/abc.txt", result["Uri"]);
        Assert.Equal("DummyAccessKey", actualAccessKey);
        Assert.Equal("DummySecretAccessKey", actualSecretAccessKey);
        Assert.NotNull(actualConfig);
        Assert.Equal("ap-northeast-1", actualConfig.RegionEndpoint.SystemName);
        Assert.False(actualConfig.UseHttp);
        Assert.Equal(new Uri("http://dng-proxy-o.denso.co.jp:8080"), ((WebProxy)actualConfig.GetWebProxy()).Address);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromFile")]
    public async Task UploadObjectFromFileErrorTest()
    {
        // arrange
        var fileInfo = TestDir.Get("UploadObjectFromFile").CombineWithFileName("abc.txt");
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            //{ "Bucket Name", "bucketName" },
            { "File Path", fileInfo.FullName },
            { "Prefix", "hoge" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObjectFromFile", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("Bucket Name", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromFile")]
    public async Task UploadObjectFromFileErrorTest2()
    {
        // arrange
        var fileInfo = TestDir.Get("UploadObjectFromFile").CombineWithFileName("abc.txt");
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            //{ "File Path", fileInfo.FullName },
            { "Prefix", "hoge" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObjectFromFile", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("File Path", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromFile")]
    public async Task UploadObjectFromFileErrorTest3()
    {
        // arrange
        var fileInfo = TestDir.Get("UploadObjectFromFile").CombineWithFileName("abc.txt");
        using var cts = new CancellationTokenSource(100000000);
        var called = false;
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                GetObjectMetadataAsyncMock = (bucketName, key, token) =>
                {
                    called = true;
                    return Task.FromResult(new GetObjectMetadataResponse());
                },
            };
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            { "File Path", fileInfo.FullName },
            //{ "Prefix", "hoge" }, <- optional
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObjectFromFile", arguments, cts.Token);

        // assert
        Assert.True(called);
        Assert.Equal(2, result["Result"]);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromFile")]
    public async Task UploadObjectFromFileErrorTest4()
    {
        // arrange
        var fileInfo = TestDir.Get("UploadObjectFromFile").CombineWithFileName("abc.txt");
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                GetObjectMetadataAsyncMock = (bucketName, key, token) =>
                {
                    Assert.Fail();
                    throw new Exception();
                },
                PutObjectAsyncMock = (request, token) =>
                {
                    Assert.Equal("hoge/abc.txt", request.Key);
                    return Task.FromResult(new PutObjectResponse() { ETag = "aaa", HttpStatusCode = HttpStatusCode.OK });
                },
            };
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            { "File Path", fileInfo.FullName },
            { "Prefix", "hoge" },
            //{ "Overwrite", false }, <- optional
        };
        var result = await controller.ExecuteAsync("UploadObjectFromFile", arguments, cts.Token);

        // assert
        Assert.Equal(0, result["Result"]);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromFile")]
    public async Task UploadObjectFromFileErrorTest5()
    {
        // arrange
        var fileInfo = TestDir.Get("UploadObjectFromFile").CombineWithFileName("abc.txt");
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            { "File Path", "notexist.txt" },
            { "Prefix", "hoge" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObjectFromFile", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("notexist.txt", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromDirectory")]
    public async Task UploadObjectFromDirectoryTest()
    {
        var dirInfo = TestDir.Get("UploadObjectFromDirectory");
        var actualAccessKey = string.Empty;
        var actualSecretAccessKey = string.Empty;
        AmazonS3Config? actualConfig = null;
        var bucketName = "mytestawsbacket2025";
        var counter = 0;
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            actualAccessKey = accessKey;
            actualSecretAccessKey = secretAccessKey;
            actualConfig = config;
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                GetObjectMetadataAsyncMock = (bucketName, key, token) =>
                {
                    throw new AmazonS3Exception("hoge", ErrorType.Unknown, "", "", HttpStatusCode.NotFound);
                },
                PutObjectAsyncMock = async (request, token) =>
                {
                    if (counter == 0)
                    {
                        ++counter;
                        Assert.Equal(bucketName, request.BucketName);
                        Assert.Equal("hoge/def.txt", request.Key);
                        request.InputStream.Seek(0, SeekOrigin.Begin);
                        Assert.Equal(3, request.InputStream.Length);
                        var buffer = new byte[3];
                        var readLength = await request.InputStream.ReadAsync(buffer, token);
                        Assert.Equal(3, readLength);
                        Assert.Equal("def", Encoding.UTF8.GetString(buffer));
                        return new PutObjectResponse() { ETag = "aaa", HttpStatusCode = HttpStatusCode.OK };
                    }
                    else if (counter == 1)
                    {
                        ++counter;
                        Assert.Equal(bucketName, request.BucketName);
                        Assert.Equal("hoge/XYZ.txt", request.Key);
                        request.InputStream.Seek(0, SeekOrigin.Begin);
                        Assert.Equal(3, request.InputStream.Length);
                        var buffer = new byte[3];
                        var readLength = await request.InputStream.ReadAsync(buffer, token);
                        Assert.Equal(3, readLength);
                        Assert.Equal("XYZ", Encoding.UTF8.GetString(buffer));
                        return new PutObjectResponse() { ETag = "aaa", HttpStatusCode = HttpStatusCode.OK };
                    }
                    else
                    {
                        Assert.Fail();
                        throw new Exception();
                    }
                },
            };
        });
        using var cts = new CancellationTokenSource(100000000);
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
            { "Use Https", true },
            { "Proxy Uri", "http://dng-proxy-o.denso.co.jp:8080" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", bucketName },
            { "Directory Path", dirInfo.FullName },
            { "Prefix", "hoge" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObjectFromDirectory", arguments, cts.Token);

        Assert.Equal(0, result["Result"]);
        Assert.Equal(2, result["Uploaded Count"]);
        Assert.Equal("DummyAccessKey", actualAccessKey);
        Assert.Equal("DummySecretAccessKey", actualSecretAccessKey);
        Assert.NotNull(actualConfig);
        Assert.Equal("ap-northeast-1", actualConfig.RegionEndpoint.SystemName);
        Assert.False(actualConfig.UseHttp);
        Assert.Equal(new Uri("http://dng-proxy-o.denso.co.jp:8080"), ((WebProxy)actualConfig.GetWebProxy()).Address);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromDirectory")]
    public async Task UploadObjectFromDirectoryErrorTest()
    {
        // arrange
        var dirInfo = TestDir.Get("UploadObjectFromDirectory");
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            //{ "Bucket Name", "bucketName" },
            { "Directory Path", dirInfo.FullName },
            { "Prefix", "hoge" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObjectFromDirectory", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("Bucket Name", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromDirectory")]
    public async Task UploadObjectFromDirectoryErrorTest2()
    {
        // arrange
        var dirInfo = TestDir.Get("UploadObjectFromDirectory");
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            //{ "Directory Path", dirInfo.FullName },
            { "Prefix", "hoge" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObjectFromDirectory", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("Directory Path", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromDirectory")]
    public async Task UploadObjectFromDirectoryErrorTest3()
    {
        // arrange
        var dirInfo = TestDir.Get("UploadObjectFromDirectory");
        using var cts = new CancellationTokenSource(100000000);
        var counter = 0;
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                PutObjectAsyncMock = (request, token) =>
                {
                    if (counter == 0)
                    {
                        ++counter;
                        return Task.FromResult(new PutObjectResponse() { ETag = "aaa", HttpStatusCode = HttpStatusCode.OK });
                    }
                    else if (counter == 1)
                    {
                        ++counter;
                        return Task.FromResult(new PutObjectResponse() { ETag = "aaa", HttpStatusCode = HttpStatusCode.OK });
                    }
                    else
                    {
                        Assert.Fail();
                        throw new Exception();
                    }
                },
            };
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            { "Directory Path", dirInfo.FullName },
            //{ "Prefix", "hoge" }, <- optional
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObjectFromDirectory", arguments, cts.Token);

        // assert
        Assert.Equal(0, result["Result"]);
        Assert.Equal(2, counter);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromDirectory")]
    public async Task UploadObjectFromDirectoryErrorTest4()
    {
        // arrange
        var dirInfo = TestDir.Get("UploadObjectFromDirectory");
        using var cts = new CancellationTokenSource(100000000);
        var counter = 0;
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                GetObjectMetadataAsyncMock = (bucketName, key, token) =>
                {
                    return Task.FromResult(new GetObjectMetadataResponse());
                },
                PutObjectAsyncMock = (request, token) =>
                {
                    if (counter == 0)
                    {
                        ++counter;
                        return Task.FromResult(new PutObjectResponse() { ETag = "aaa", HttpStatusCode = HttpStatusCode.OK });
                    }
                    else if (counter == 1)
                    {
                        ++counter;
                        return Task.FromResult(new PutObjectResponse() { ETag = "aaa", HttpStatusCode = HttpStatusCode.OK });
                    }
                    else
                    {
                        Assert.Fail();
                        throw new Exception();
                    }
                },
            };
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            { "Directory Path", dirInfo.FullName },
            { "Prefix", "hoge" },
            // { "Overwrite", false }, // <- optional
        };
        var result = await controller.ExecuteAsync("UploadObjectFromDirectory", arguments, cts.Token);

        // assert
        Assert.Equal(0, result["Result"]);
        Assert.Equal(2, counter);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.UploadObjectFromDirectory")]
    public async Task UploadObjectFromDirectoryErrorTest5()
    {
        // arrange
        var dirInfo = TestDir.Get("notexists");
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            { "Directory Path", dirInfo.FullName },
            { "Prefix", "hoge" },
            { "Overwrite", false },
        };
        var result = await controller.ExecuteAsync("UploadObjectFromDirectory", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains(dirInfo.FullName, (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.ListObjects")]
    public async Task ListObjectsTest()
    {
        var actualAccessKey = string.Empty;
        var actualSecretAccessKey = string.Empty;
        AmazonS3Config? actualConfig = null;
        var bucketName = "mytestawsbacket2025";
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            actualAccessKey = accessKey;
            actualSecretAccessKey = secretAccessKey;
            actualConfig = config;
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                ListObjectsV2AsyncMock = (request, token) =>
                {
                    Assert.Equal(bucketName, request.BucketName);
                    Assert.Equal("hoge", request.Prefix);
                    var list = new List<S3Object>() { new() { Key = "aaa" }, new() { Key = "bbb" } };
                    return Task.FromResult(new ListObjectsV2Response() { HttpStatusCode = HttpStatusCode.OK, KeyCount = list.Count, S3Objects = list });
                }
            };
        });
        using var cts = new CancellationTokenSource(100000000);
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
            { "Use Https", true },
            { "Proxy Uri", "http://dng-proxy-o.denso.co.jp:8080" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", bucketName },
            { "Prefix", "hoge" },
        };
        var result = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);

        Assert.Equal(0, result["Result"]);
        Assert.Equal(2, result["Object Count"]);
        Assert.Equal(new string[] { "aaa", "bbb" }, (string[])result["Object Names"]!);
        Assert.Equal("DummyAccessKey", actualAccessKey);
        Assert.Equal("DummySecretAccessKey", actualSecretAccessKey);
        Assert.NotNull(actualConfig);
        Assert.Equal("ap-northeast-1", actualConfig.RegionEndpoint.SystemName);
        Assert.False(actualConfig.UseHttp);
        Assert.Equal(new Uri("http://dng-proxy-o.denso.co.jp:8080"), ((WebProxy)actualConfig.GetWebProxy()).Address);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.ListObjects")]
    public async Task ListObjectsTest2()
    {
        var actualAccessKey = string.Empty;
        var actualSecretAccessKey = string.Empty;
        AmazonS3Config? actualConfig = null;
        var bucketName = "mytestawsbacket2025";
        var counter = 0;
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            actualAccessKey = accessKey;
            actualSecretAccessKey = secretAccessKey;
            actualConfig = config;
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                ListObjectsV2AsyncMock = (request, token) =>
                {
                    if (counter == 0)
                    {
                        ++counter;
                        Assert.Equal(bucketName, request.BucketName);
                        Assert.Equal("hoge", request.Prefix);
                        Assert.Null(request.ContinuationToken);
                        var list = new List<S3Object>() { new() { Key = "aaa" }, new() { Key = "bbb" } };
                        return Task.FromResult(new ListObjectsV2Response() { HttpStatusCode = HttpStatusCode.Accepted, KeyCount = list.Count, S3Objects = list, NextContinuationToken = "1" });
                    }
                    else if (counter == 1)
                    {
                        ++counter;
                        Assert.Equal(bucketName, request.BucketName);
                        Assert.Equal("hoge", request.Prefix);
                        Assert.Equal("1", request.ContinuationToken);
                        var list = new List<S3Object>() { new() { Key = "ccc" }, new() { Key = "ddd" } };
                        return Task.FromResult(new ListObjectsV2Response() { HttpStatusCode = HttpStatusCode.OK, KeyCount = list.Count, S3Objects = list });
                    }
                    else
                    {
                        Assert.Fail();
                        throw new Exception();
                    }
                }
            };
        });
        using var cts = new CancellationTokenSource(100000000);
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
            { "Use Https", true },
            { "Proxy Uri", "http://dng-proxy-o.denso.co.jp:8080" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", bucketName },
            { "Prefix", "hoge" },
        };
        var result = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);

        Assert.Equal(0, result["Result"]);
        Assert.Equal(4, result["Object Count"]);
        Assert.Equal((int)HttpStatusCode.OK, result["HTTP Status"]);
        Assert.Equal(new string[] { "aaa", "bbb", "ccc", "ddd" }, (string[])result["Object Names"]!);
        Assert.Equal("DummyAccessKey", actualAccessKey);
        Assert.Equal("DummySecretAccessKey", actualSecretAccessKey);
        Assert.NotNull(actualConfig);
        Assert.Equal("ap-northeast-1", actualConfig.RegionEndpoint.SystemName);
        Assert.False(actualConfig.UseHttp);
        Assert.Equal(new Uri("http://dng-proxy-o.denso.co.jp:8080"), ((WebProxy)actualConfig.GetWebProxy()).Address);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.ListObjects")]
    public async Task ListObjectsErrorTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            //{ "Bucket Name", "bucketName" },
            { "Prefix", "hoge" },
        };
        var result = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("Bucket Name", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.ListObjects")]
    public async Task ListObjectsErrorTest2()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            //{ "Prefix", "hoge" }, // <- optional
        };
        var result = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);

        // assert
        Assert.Equal(0, result["Result"]);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.DeleteObject")]
    public async Task DeleteObjectTest()
    {
        var actualAccessKey = string.Empty;
        var actualSecretAccessKey = string.Empty;
        AmazonS3Config? actualConfig = null;
        var bucketName = "mytestawsbacket2025";
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            actualAccessKey = accessKey;
            actualSecretAccessKey = secretAccessKey;
            actualConfig = config;
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config)
            {
                DeleteObjectAsyncMock = (request, token) =>
                {
                    Assert.Equal(bucketName, request.BucketName);
                    Assert.Equal("hoge.txt", request.Key);
                    Assert.Equal("abc", request.VersionId);
                    return Task.FromResult(new DeleteObjectResponse() { VersionId = request.VersionId, HttpStatusCode = HttpStatusCode.OK });
                },
            };
        });
        using var cts = new CancellationTokenSource(100000000);
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
            { "Use Https", true },
            { "Proxy Uri", "http://dng-proxy-o.denso.co.jp:8080" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", bucketName },
            { "Object Key", "hoge.txt" },
            { "Version Id", "abc" },
        };
        var result = await controller.ExecuteAsync("DeleteObject", arguments, cts.Token);

        Assert.Equal(0, result["Result"]);
        Assert.Equal("hoge.txt", result["Object Key"]);
        Assert.Equal("abc", result["Version Id"]);
        Assert.Equal((int)HttpStatusCode.OK, result["HTTP Status"]);
        Assert.Equal("DummyAccessKey", actualAccessKey);
        Assert.Equal("DummySecretAccessKey", actualSecretAccessKey);
        Assert.NotNull(actualConfig);
        Assert.Equal("ap-northeast-1", actualConfig.RegionEndpoint.SystemName);
        Assert.False(actualConfig.UseHttp);
        Assert.Equal(new Uri("http://dng-proxy-o.denso.co.jp:8080"), ((WebProxy)actualConfig.GetWebProxy()).Address);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.DeleteObject")]
    public async Task DeleteObjectErrorTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            //{ "Bucket Name", "bucketName" },
            { "Object Key", "hoge.txt" },
            { "Version Id", "abc" },
        };
        var result = await controller.ExecuteAsync("DeleteObject", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("Bucket Name", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.DeleteObject")]
    public async Task DeleteObjectErrorTest2()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            //{ "Object Key", "hoge.txt" },
            { "Version Id", "abc" },
        };
        var result = await controller.ExecuteAsync("DeleteObject", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.Contains("Object Key", (string)result["Error Message"]!);
    }

    [Fact]
    [Trait("Category", $"{nameof(S3StorageController)}.DeleteObject")]
    public async Task DeleteObjectErrorTest3()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        using var methodReverter = AmazonS3ClientEx.SetCreateMethod((accessKey, secretAccessKey, config) =>
        {
            return new AmazonS3ClientMock(accessKey, secretAccessKey, config);
        });
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "DummyAccessKey" },
            { "Secret Access Key", "3af2LIOi9aDwYDjBCWqT40gH39vrA1SnX9Th2Q64fgHMmf1y8qsbECee/59Rtsi664kf5eZPnfpscdXJvM83HQ==" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "bucketName" },
            { "Object Key", "hoge.txt" },
            //{ "Version Id", "abc" }, <- optional
        };
        var result = await controller.ExecuteAsync("DeleteObject", arguments, cts.Token);

        // assert
        Assert.Equal(0, result["Result"]);
    }

    [Fact]
    public async Task Test()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        var bucketName = "mytestawsbacket2025";
        var option = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Region Endpoint", "ap-northeast-1" },
            { "Access Key", "AKIAY5JCCRAUNLJFHGTZ" },
            { "Secret Access Key", "DrlVgRBq89KLKKjRk0nN03Vmvoh4uQ/GlpI4zmYHkfqKtiaKuExfH34zJByI0i3rscG6j9kYsBEfuYBwYth4rqLrOccqLf4Ca74k0bZtAGE=" },
            { "Use Https", true },
            { "Proxy Uri", "http://dng-proxy-o.denso.co.jp:8080" },
        };
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option),
            token: cts.Token);

        { // clean up
            var arguments = new Dictionary<string, object?>()
            {
                { "Bucket Name", bucketName },
                { "Prefix", string.Empty },
            };
            var result = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);
            Assert.Equal(0, result["Result"]);

            foreach (var key in (string[])result["Object Names"]!)
            {
                var artument = new Dictionary<string, object?>()
                {
                    { "Bucket Name", bucketName },
                    { "Object Key", key },
                };
                var result2 = await controller.ExecuteAsync("DeleteObject", artument, cts.Token);
                Assert.Equal(0, result2["Result"]);
            }
        }

        { // list
            var arguments = new Dictionary<string, object?>()
            {
                { "Bucket Name", bucketName },
                { "Prefix", string.Empty },
            };
            var result = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);
            Assert.Equal(0, result["Result"]);
            Assert.Equal(0, result["Object Count"]);

            // upload
            var arguments2 = new Dictionary<string, object?>()
            {
                { "Bucket Name", bucketName },
                { "Bytes", Encoding.UTF8.GetBytes("abcde") },
                { "Object Key", "hogehoge2.txt" },
                { "Overwrite", false },
            };
            var result2 = await controller.ExecuteAsync("UploadObject", arguments2, cts.Token);
            Assert.Equal(0, result2["Result"]);
            var result3 = await controller.ExecuteAsync("UploadObject", arguments2, cts.Token);
            Assert.Equal(2, result3["Result"]);

            var result4 = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);
            Assert.Equal(0, result4["Result"]);
            Assert.Equal(1, result4["Object Count"]);
            Assert.Single((string[])result4["Object Names"]!);
            Assert.Equal("hogehoge2.txt", ((string[])result4["Object Names"]!)[0]);
        }

        var option2 = new Dictionary<string, object>()
        {
            { "@Version", "0.0.1" },
            { "Bucket Name", bucketName },
            { "Object Key", "hogehoge2.txt" },
        };
        var file = await controller.CreateFileAsync(
            name: "S3File",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.File.S3ObjectFile, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: JsonSerializer.Serialize(option2),
            token: cts.Token);

        await file.OpenAsync();
        var buffer = new byte[5];
        await file.ReadAsync(buffer, cts.Token);
        var length = await file.GetLengthAsync(cts.Token);

        var actDel = await controller.ExecuteAsync("DeleteObject", new Dictionary<string, object?>()
        {
            { "Bucket Name", bucketName },
            { "Object Key", "hogehoge2.txt" },
        }, cts.Token);

        var actual1 = await controller.ExecuteAsync("UploadObject", new Dictionary<string, object?>
        {
            { "Bucket Name", bucketName },
            { "Bytes", new byte[] { 1, 2, 3, 4, 5 } },
            { "Object Key", "hogehoge2.txt" },
            { "Overwrite", false },
        }, cts.Token);


    }
}
