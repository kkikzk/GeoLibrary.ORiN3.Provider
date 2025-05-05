using Azure;
using Azure.Storage.Blobs.Models;
using Colda.CommonUtilities.IO;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;
using GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;
using GeoLibrary.ORiN3.Provider.Azure.Storage.Test.TestUtilities;
using GeoLibrary.ORiN3.Provider.TestBaseLib;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.O3Object.Controller;

public class BlobStorageControllerTest : IClassFixture<ProviderTestFixture<BlobStorageControllerTest>>, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ProviderTestFixture<BlobStorageControllerTest> _fixture;
    private readonly CancellationTokenSource _tokenSource = new(10000);

    public BlobStorageControllerTest(ProviderTestFixture<BlobStorageControllerTest> fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.InitAsync<AzureRootObjectForTest>(_output, _tokenSource.Token).Wait();
    }

    public void Dispose()
    {
        try
        {
            _tokenSource.Dispose();
        }
        catch
        {
            // do nothing
        }
        GC.SuppressFinalize(this);
    }

    [Theory(DisplayName = nameof(ControllerOptionTest))]
    [Trait("Category", "BlobStorageController")]
    [InlineData("iotsolution1", "YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj", "true", "core.windows.net", "DefaultEndpointsProtocol=https;AccountName=iotsolution1;AccountKey=testKey;EndpointSuffix=core.windows.net")]
    [InlineData("hogefuga", "CZnsEFFRZO7dX9nuWVXVo9H5MSs778fhxZRKUTRiSvdg7jMy8xzWVlrlDuXH6tq2", "false", "hoge.net", "DefaultEndpointsProtocol=http;AccountName=hogefuga;AccountKey=testKey2;EndpointSuffix=hoge.net")]
    public async Task ControllerOptionTest(string accountName, string accountKey, string useHttps, string endpointSuffix, string expected)
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var actualConnectionString = string.Empty;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) =>
        {
            actualConnectionString = connectionString;
            return new BlobContainerClientMock(connectionString, containerName);
        });

        // act
        var sut = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"" + accountName + "\",\"Account Key\":\"" + accountKey + "\",\"Use Https\":" + useHttps + ",\"Endpoint Suffix\":\"" + endpointSuffix + "\"}",
            token: cts.Token);
        var arguments = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc" },
            { "Overwrite", true },
        };
        _ = await sut.ExecuteAsync("UploadBlock", arguments, cts.Token);

        // assert
        Assert.Equal(expected, actualConnectionString);
        await sut.DeleteAsync(cts.Token);
    }

    [Theory]
    [Trait("Category", "BlobStorageController.UploadBlock")]
    [InlineData("{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"c6OZIVO5OxjhO81qy8MEhCE6haxaNSIugpW/9QEQmfAK2ylTZ\\u002B1fAidpEG0nrnUd\"}", "DefaultEndpointsProtocol=https;AccountName=iotsolution1;AccountKey=testKey;EndpointSuffix=core.windows.net")]
    [InlineData("{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"c6OZIVO5OxjhO81qy8MEhCE6haxaNSIugpW/9QEQmfAK2ylTZ\\u002B1fAidpEG0nrnUd\",\"Use Https\":true}", "DefaultEndpointsProtocol=https;AccountName=iotsolution1;AccountKey=testKey;EndpointSuffix=core.windows.net")]
    [InlineData("{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"c6OZIVO5OxjhO81qy8MEhCE6haxaNSIugpW/9QEQmfAK2ylTZ\\u002B1fAidpEG0nrnUd\",\"Endpoint Suffix\":\"core.windows.net\"}", "DefaultEndpointsProtocol=https;AccountName=iotsolution1;AccountKey=testKey;EndpointSuffix=core.windows.net")]
    public async Task OptionalOptionTest(string option, string expected)
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var actualConnectionString = string.Empty;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) =>
        {
            actualConnectionString = connectionString;
            return new BlobContainerClientMock(connectionString, containerName);
        });

        // act
        var sut = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: option,
            token: cts.Token);
        var arguments = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc" },
            { "Overwrite", true },
        };
        _ = await sut.ExecuteAsync("UploadBlock", arguments, cts.Token);

        // assert
        Assert.Equal(expected, actualConnectionString);
        await sut.DeleteAsync(cts.Token);
    }

    #region Upload Block
    [Theory]
    [Trait("Category", "BlobStorageController.UploadBlock")]
    [InlineData("testContainerName", "abc.txt", new byte[] { 1, 2, 3 }, true)]
    [InlineData("testContainerName2", "a/b/c/d/e/xyz.png", new byte[0], false)]
    public async Task UploadBlockTest(string containerName, string blobPath, byte[] data, bool overwrite)
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var createIfNotExistsAsyncMockCalled = false;
        var now = DateTime.Now;
        var eTag = new ETag(Guid.NewGuid().ToString());
        var actualContainerName = string.Empty;
        var actualBlobPath = string.Empty;
        var actualData = Array.Empty<byte>();
        var actualOverwrite = false;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) =>
        {
            actualContainerName = containerName;
            return new BlobContainerClientMock(connectionString, containerName)
            {
                CreateIfNotExistsAsyncMock = (token) =>
                {
                    createIfNotExistsAsyncMockCalled = true;
                    var info = BlobsModelFactory.BlobContainerInfo(
                        lastModified: new DateTimeOffset(now),
                        eTag: eTag
                    );
                    return Task.FromResult(Response.FromValue(info, null!));
                },
                GetBlobClientMock = (blobName) =>
                {
                    actualBlobPath = blobName;
                    return new BlobClientMock(blobName)
                    {
                        UploadAsyncMock = (stream, overwrite, token) =>
                        {
                            actualData = stream.ToArray();
                            actualOverwrite = overwrite;
                            var info = BlobsModelFactory.BlobContentInfo(
                                eTag: eTag,
                                lastModified: new DateTimeOffset(now),
                                contentHash: [],
                                versionId: string.Empty,
                                encryptionKeySha256: string.Empty,
                                encryptionScope: string.Empty,
                                blobSequenceNumber: 0
                            );
                            return Task.FromResult(Response.FromValue(info, null!));
                        }
                    };
                }
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bytes", data },
            { "Container Name", containerName },
            { "Blob Path", blobPath },
            { "Overwrite", overwrite },
        };
        var result = await controller.ExecuteAsync("UploadBlock", arguments, cts.Token);

        // assert
        Assert.Equal(containerName, actualContainerName);
        Assert.Equal(blobPath, actualBlobPath);
        Assert.Equal(data, actualData);
        Assert.Equal(overwrite, actualOverwrite);
        Assert.True(createIfNotExistsAsyncMockCalled);
        Assert.Equal(0, result["Result"]);
        Assert.Equal(blobPath, result["Blob Path"]);
        Assert.Equal("http://hoge.com/", result["Uri"]);
        Assert.Equal(eTag.ToString(), result["ETag"]);
        Assert.Equal(now.ToUniversalTime(), result["Last Modified"]);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlock")]
    public async Task UploadBlockResultTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Data", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "Overwrite", true },
        };
        var result = await controller.ExecuteAsync("UploadBlock", arguments, cts.Token);

        // assert
        Assert.Equal(9, result.Count);
        Assert.True(result.ContainsKey("Result"));
        Assert.Equal(typeof(int), result["Result"]!.GetType());
        Assert.True(result.ContainsKey("Blob Path"));
        Assert.Equal(typeof(string), result["Blob Path"]!.GetType());
        Assert.True(result.ContainsKey("Uri"));
        Assert.Equal(typeof(string), result["Uri"]!.GetType());
        Assert.True(result.ContainsKey("ETag"));
        Assert.Equal(typeof(string), result["ETag"]!.GetType());
        Assert.True(result.ContainsKey("Last Modified"));
        Assert.Equal(typeof(DateTime), result["Last Modified"]!.GetType());
        Assert.True(result.ContainsKey("Azure Error Code"));
        Assert.Equal(typeof(string), result["Azure Error Code"]!.GetType());
        Assert.True(result.ContainsKey("HTTP Status"));
        Assert.Equal(typeof(int), result["HTTP Status"]!.GetType());
        Assert.True(result.ContainsKey("Error Message"));
        Assert.Equal(typeof(string), result["Error Message"]!.GetType());
        Assert.True(result.ContainsKey("Stack Trace"));
        Assert.Equal(typeof(string), result["Stack Trace"]!.GetType());
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlock")]
    public async Task UploadBlockWithInvalidOptionTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // lack of option
        // act
        var arguments1 = new Dictionary<string, object?>()
        {
            //{ "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "Overwrite", true },
        };
        var result1 = await controller.ExecuteAsync("UploadBlock", arguments1, cts.Token);

        // assert
        Assert.Equal(2, result1["Result"]);
        Assert.Equal("Invalid argument. \"Bytes\" does not exist.", result1["Error Message"]);
        Assert.NotEqual(string.Empty, result1["Stack Trace"]);
        Assert.Equal(9, result1.Count);

        // act
        var arguments2 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            //{ "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "Overwrite", true },
        };
        var result2 = await controller.ExecuteAsync("UploadBlock", arguments2, cts.Token);

        // assert
        Assert.Equal(2, result2["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" does not exist.", result2["Error Message"]);
        Assert.NotEqual(string.Empty, result2["Stack Trace"]);
        Assert.Equal(9, result2.Count);

        // act
        var arguments3 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            //{ "Blob Path", "abc.txt" },
            { "Overwrite", true },
        };
        var result3 = await controller.ExecuteAsync("UploadBlock", arguments3, cts.Token);

        // assert
        Assert.Equal(2, result3["Result"]);
        Assert.Equal("Invalid argument. \"Blob Path\" does not exist.", result3["Error Message"]);
        Assert.NotEqual(string.Empty, result3["Stack Trace"]);
        Assert.Equal(9, result3.Count);

        // act
        var arguments4 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            //{ "Overwrite", true }, <= optional option
        };
        var result4 = await controller.ExecuteAsync("UploadBlock", arguments4, cts.Token);

        // assert
        Assert.Equal(0, result4["Result"]);
        Assert.Equal(string.Empty, result4["Error Message"]);
        Assert.Equal(string.Empty, result4["Stack Trace"]);
        Assert.Equal(9, result4.Count);

        // invalid type of option
        // act
        var arguments5 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<int>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "Overwrite", true },
        };
        var result5 = await controller.ExecuteAsync("UploadBlock", arguments5, cts.Token);

        // assert
        Assert.Equal(2, result5["Result"]);
        Assert.Equal("Invalid argument. \"Bytes\" is not Byte[].", result5["Error Message"]);
        Assert.NotEqual(string.Empty, result5["Stack Trace"]);
        Assert.Equal(9, result5.Count);

        // act
        var arguments6 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", 0 },
            { "Blob Path", "abc.txt" },
            { "Overwrite", true },
        };
        var result6 = await controller.ExecuteAsync("UploadBlock", arguments6, cts.Token);

        // assert
        Assert.Equal(2, result6["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" is not String.", result6["Error Message"]);
        Assert.NotEqual(string.Empty, result6["Stack Trace"]);
        Assert.Equal(9, result6.Count);

        // act
        var arguments7 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", 0 },
            { "Overwrite", true },
        };
        var result7 = await controller.ExecuteAsync("UploadBlock", arguments7, cts.Token);

        // assert
        Assert.Equal(2, result7["Result"]);
        Assert.Equal("Invalid argument. \"Blob Path\" is not String.", result7["Error Message"]);
        Assert.NotEqual(string.Empty, result7["Stack Trace"]);
        Assert.Equal(9, result7.Count);

        // act
        var arguments8 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "Overwrite", 0 },
        };
        var result8 = await controller.ExecuteAsync("UploadBlock", arguments8, cts.Token);

        // assert
        Assert.Equal(2, result8["Result"]);
        Assert.Equal("Invalid argument. \"Overwrite\" is not Boolean.", result8["Error Message"]);
        Assert.NotEqual(string.Empty, result8["Stack Trace"]);
        Assert.Equal(9, result8.Count);

        // tear down
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlock")]
    public async Task UploadBlockErrorFromAzureTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName)
        {
            GetBlobClientMock = (blobName) => new BlobClientMock(blobName)
            {
                UploadAsyncMock = (stream, overwrite, token) => throw new RequestFailedException(status: 1, errorCode: "hoge", message: "fuga", innerException: null)
            }
        });

        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "containerName" },
            { "Blob Path", "abc.txt" },
            { "Overwrite", true },
        };
        var result = await controller.ExecuteAsync("UploadBlock", arguments, cts.Token);

        // assert
        Assert.Equal(1, result["Result"]);
        Assert.Equal("hoge", result["Azure Error Code"]);
        Assert.Equal(1, result["HTTP Status"]);
        Assert.NotEqual(string.Empty, result["Stack Trace"]);
        Assert.NotNull(result["Stack Trace"]);
        await controller.DeleteAsync(cts.Token);
    }
    #endregion

    #region Upload Block From File
    [Theory]
    [Trait("Category", "BlobStorageController.UploadBlockFromFile")]
    [InlineData("testContainerName", "abc.txt", "", true, "abc.txt")]
    [InlineData("testContainerName2", "vwxyz.txt", "a/b/c/", false, "a/b/c/vwxyz.txt")]
    [InlineData("testContainerName2", "vwxyz.txt", "A", false, "A/vwxyz.txt")]
    public async Task UploadBlockFromFileTest(string containerName, string fileName, string prefix, bool overwrite, string expectedBlobPath)
    {
        // arrange
        var fileInfo = TestDir.Get("UploadBlockFromFile").CombineWithFileName(fileName);
        using var cts = new CancellationTokenSource(10000);
        var createIfNotExistsAsyncMockCalled = false;
        var now = DateTime.Now;
        var eTag = new ETag(Guid.NewGuid().ToString());
        var actualContainerName = string.Empty;
        var actualBlobPath = string.Empty;
        var actualData = Array.Empty<byte>();
        var actualOverwrite = false;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) =>
        {
            actualContainerName = containerName;
            return new BlobContainerClientMock(connectionString, containerName)
            {
                CreateIfNotExistsAsyncMock = (token) =>
                {
                    createIfNotExistsAsyncMockCalled = true;
                    var info = BlobsModelFactory.BlobContainerInfo(
                        lastModified: new DateTimeOffset(now),
                        eTag: eTag
                    );
                    return Task.FromResult(Response.FromValue(info, null!));
                },
                GetBlobClientMock = (blobName) =>
                {
                    actualBlobPath = blobName;
                    return new BlobClientMock(blobName)
                    {
                        UploadAsyncMock = (stream, overwrite, token) =>
                        {
                            actualData = stream.ToArray();
                            actualOverwrite = overwrite;
                            var info = BlobsModelFactory.BlobContentInfo(
                                eTag: eTag,
                                lastModified: new DateTimeOffset(now),
                                contentHash: [],
                                versionId: string.Empty,
                                encryptionKeySha256: string.Empty,
                                encryptionScope: string.Empty,
                                blobSequenceNumber: 0
                            );
                            return Task.FromResult(Response.FromValue(info, null!));
                        }
                    };
                }
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "File Path", fileInfo.FullName },
            { "Container Name", containerName },
            { "Prefix", prefix },
            { "Overwrite", overwrite },
        };
        var result = await controller.ExecuteAsync("UploadBlockFromFile", arguments, cts.Token);

        // assert
        Assert.Equal(containerName, actualContainerName);
        Assert.Equal(expectedBlobPath, actualBlobPath);
        Assert.Equal(fileInfo.ToArray(), actualData);
        Assert.Equal(overwrite, actualOverwrite);
        Assert.True(createIfNotExistsAsyncMockCalled);
        Assert.Equal(0, result["Result"]);
        Assert.Equal(expectedBlobPath, result["Blob Path"]);
        Assert.Equal("http://hoge.com/", result["Uri"]);
        Assert.Equal(eTag.ToString(), result["ETag"]);
        Assert.Equal(now.ToUniversalTime(), result["Last Modified"]);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlockFromFile")]
    public async Task UploadBlockFromFileResultTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Data", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "Overwrite", true },
        };
        var result = await controller.ExecuteAsync("UploadBlockFromFile", arguments, cts.Token);

        // assert
        Assert.Equal(9, result.Count);
        Assert.True(result.ContainsKey("Result"));
        Assert.Equal(typeof(int), result["Result"]!.GetType());
        Assert.True(result.ContainsKey("Blob Path"));
        Assert.Equal(typeof(string), result["Blob Path"]!.GetType());
        Assert.True(result.ContainsKey("Uri"));
        Assert.Equal(typeof(string), result["Uri"]!.GetType());
        Assert.True(result.ContainsKey("ETag"));
        Assert.Equal(typeof(string), result["ETag"]!.GetType());
        Assert.True(result.ContainsKey("Last Modified"));
        Assert.Equal(typeof(DateTime), result["Last Modified"]!.GetType());
        Assert.True(result.ContainsKey("Azure Error Code"));
        Assert.Equal(typeof(string), result["Azure Error Code"]!.GetType());
        Assert.True(result.ContainsKey("HTTP Status"));
        Assert.Equal(typeof(int), result["HTTP Status"]!.GetType());
        Assert.True(result.ContainsKey("Error Message"));
        Assert.Equal(typeof(string), result["Error Message"]!.GetType());
        Assert.True(result.ContainsKey("Stack Trace"));
        Assert.Equal(typeof(string), result["Stack Trace"]!.GetType());
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlockFromFile")]
    public async Task UploadBlockFromFileWithInvalidOptionTest()
    {
        // arrange
        var fileInfo = TestDir.Get("UploadBlockFromFile").CombineWithFileName("abc.txt");
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // lack of option
        // act
        var arguments1 = new Dictionary<string, object?>()
        {
            //{ "File Path", fileInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result1 = await controller.ExecuteAsync("UploadBlockFromFile", arguments1, cts.Token);

        // assert
        Assert.Equal(2, result1["Result"]);
        Assert.Equal("Invalid argument. \"File Path\" does not exist.", result1["Error Message"]);
        Assert.NotEqual(string.Empty, result1["Stack Trace"]);
        Assert.Equal(9, result1.Count);

        // act
        var arguments2 = new Dictionary<string, object?>()
        {
            { "File Path", fileInfo.FullName },
            //{ "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result2 = await controller.ExecuteAsync("UploadBlockFromFile", arguments2, cts.Token);

        // assert
        Assert.Equal(2, result2["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" does not exist.", result2["Error Message"]);
        Assert.NotEqual(string.Empty, result2["Stack Trace"]);
        Assert.Equal(9, result2.Count);

        // act
        var arguments3 = new Dictionary<string, object?>()
        {
            { "File Path", fileInfo.FullName },
            { "Container Name", "testContainerName" },
            //{ "Prefix", "hoge" }, <= optional option
            { "Overwrite", true },
        };
        var result3 = await controller.ExecuteAsync("UploadBlockFromFile", arguments3, cts.Token);

        // assert
        Assert.Equal(0, result3["Result"]);
        Assert.Equal(string.Empty, result3["Error Message"]);
        Assert.Equal(string.Empty, result3["Stack Trace"]);
        Assert.Equal(9, result3.Count);

        // act
        var arguments4 = new Dictionary<string, object?>()
        {
            { "File Path", fileInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            //{ "Overwrite", true }, <= optional option
        };
        var result4 = await controller.ExecuteAsync("UploadBlockFromFile", arguments4, cts.Token);

        // assert
        Assert.Equal(0, result4["Result"]);
        Assert.Equal(string.Empty, result4["Error Message"]);
        Assert.Equal(string.Empty, result4["Stack Trace"]);
        Assert.Equal(9, result4.Count);

        // invalid type of option
        // act
        var arguments5 = new Dictionary<string, object?>()
        {
            { "File Path", 0 },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result5 = await controller.ExecuteAsync("UploadBlockFromFile", arguments5, cts.Token);

        // assert
        Assert.Equal(2, result5["Result"]);
        Assert.Equal("Invalid argument. \"File Path\" is not String.", result5["Error Message"]);
        Assert.NotEqual(string.Empty, result5["Stack Trace"]);
        Assert.Equal(9, result5.Count);

        // act
        var arguments6 = new Dictionary<string, object?>()
        {
            { "File Path", fileInfo.FullName },
            { "Container Name", 0 },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result6 = await controller.ExecuteAsync("UploadBlockFromFile", arguments6, cts.Token);

        // assert
        Assert.Equal(2, result6["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" is not String.", result6["Error Message"]);
        Assert.NotEqual(string.Empty, result6["Stack Trace"]);
        Assert.Equal(9, result6.Count);

        // act
        var arguments7 = new Dictionary<string, object?>()
        {
            { "File Path", fileInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", 0 },
            { "Overwrite", true },
        };
        var result7 = await controller.ExecuteAsync("UploadBlockFromFile", arguments7, cts.Token);

        // assert
        Assert.Equal(2, result7["Result"]);
        Assert.Equal("Invalid argument. \"Prefix\" is not String.", result7["Error Message"]);
        Assert.NotEqual(string.Empty, result7["Stack Trace"]);
        Assert.Equal(9, result7.Count);

        // act
        var arguments8 = new Dictionary<string, object?>()
        {
            { "File Path", fileInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", 0 },
        };
        var result8 = await controller.ExecuteAsync("UploadBlockFromFile", arguments8, cts.Token);

        // assert
        Assert.Equal(2, result8["Result"]);
        Assert.Equal("Invalid argument. \"Overwrite\" is not Boolean.", result8["Error Message"]);
        Assert.NotEqual(string.Empty, result8["Stack Trace"]);
        Assert.Equal(9, result8.Count);

        // tear down
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlockFromFile")]
    public async Task UploadBlockFromFileErrorFromAzureTest()
    {
        // arrange
        var fileInfo = TestDir.Get("UploadBlockFromFile").CombineWithFileName("abc.txt");
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName)
        {
            GetBlobClientMock = (blobName) => new BlobClientMock(blobName)
            {
                UploadAsyncMock = (stream, overwrite, token) => throw new RequestFailedException(status: 1, errorCode: "hoge", message: "fuga", innerException: null)
            }
        });

        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "File Path", fileInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result = await controller.ExecuteAsync("UploadBlockFromFile", arguments, cts.Token);

        // assert
        Assert.Equal(1, result["Result"]);
        Assert.Equal("hoge", result["Azure Error Code"]);
        Assert.Equal(1, result["HTTP Status"]);
        Assert.NotEqual(string.Empty, result["Stack Trace"]);
        Assert.NotNull(result["Stack Trace"]);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlockFromFile")]
    public async Task UploadBlockFromFileErrorNoFile()
    {
        // arrange
        var fileInfo = TestDir.Get("UploadBlockFromFile").CombineWithFileName("void.txt");
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName)
        {
            GetBlobClientMock = (blobName) => new BlobClientMock(blobName)
            {
                UploadAsyncMock = (stream, overwrite, token) => throw new RequestFailedException(status: 1, errorCode: "hoge", message: "fuga", innerException: null)
            }
        });

        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "File Path", fileInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result = await controller.ExecuteAsync("UploadBlockFromFile", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.NotEqual(string.Empty, result["Error Message"]);
        Assert.NotNull(result["Error Message"]);
        Assert.NotEqual(string.Empty, result["Stack Trace"]);
        Assert.NotNull(result["Stack Trace"]);
        await controller.DeleteAsync(cts.Token);
    }
    #endregion

    #region Upload Block From Directory
    [Theory]
    [Trait("Category", "BlobStorageController.UploadBlockFromDirectory")]
    [InlineData("testContainerName", "", true, new[] { "def.txt", "XYZ.txt" })]
    [InlineData("testContainerName2", "a/b/c/", false, new[] { "a/b/c/def.txt", "a/b/c/XYZ.txt" })]
    [InlineData("testContainerName2", "A", false, new[] { "A/def.txt", "A/XYZ.txt" })]
    public async Task UploadBlockFromDirectoryTest(string containerName, string prefix, bool overwrite, string[] expectedBlobPath)
    {
        // arrange
        var dirInfo = TestDir.Get("UploadBlockFromDirectory");
        using var cts = new CancellationTokenSource(10000);
        var createIfNotExistsAsyncMockCalled = false;
        var now = DateTime.Now;
        var eTag = new ETag(Guid.NewGuid().ToString());
        var actualContainerName = string.Empty;
        var actualBlobPath = new List<string>();
        var actualData = new List<byte[]>();
        var actualOverwrite = new List<bool>();
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) =>
        {
            actualContainerName = containerName;
            return new BlobContainerClientMock(connectionString, containerName)
            {
                CreateIfNotExistsAsyncMock = (token) =>
                {
                    createIfNotExistsAsyncMockCalled = true;
                    var info = BlobsModelFactory.BlobContainerInfo(
                        lastModified: new DateTimeOffset(now),
                        eTag: eTag
                    );
                    return Task.FromResult(Response.FromValue(info, null!));
                },
                GetBlobClientMock = (blobName) =>
                {
                    actualBlobPath.Add(blobName);
                    return new BlobClientMock(blobName)
                    {
                        UploadAsyncMock = (stream, overwrite, token) =>
                        {
                            actualData.Add(stream.ToArray());
                            actualOverwrite.Add(overwrite);
                            var info = BlobsModelFactory.BlobContentInfo(
                                eTag: eTag,
                                lastModified: new DateTimeOffset(now),
                                contentHash: [],
                                versionId: string.Empty,
                                encryptionKeySha256: string.Empty,
                                encryptionScope: string.Empty,
                                blobSequenceNumber: 0
                            );
                            return Task.FromResult(Response.FromValue(info, null!));
                        }
                    };
                }
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Directory Path", dirInfo.FullName },
            { "Container Name", containerName },
            { "Prefix", prefix },
            { "Overwrite", overwrite },
        };
        var result = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments, cts.Token);

        // assert
        Assert.Equal(containerName, actualContainerName);
        Assert.Equal(expectedBlobPath.Order(), actualBlobPath.Order());
        foreach (var path in expectedBlobPath)
        {
            var index = actualBlobPath.FindIndex(_ => _ == path);
            Assert.Equal(dirInfo.CombineWithFileName(new FileInfo(path).Name).ToArray(), actualData[index]);
        }
        Assert.Equal(2, actualOverwrite.Where(_ => _ == overwrite).Count());
        Assert.True(createIfNotExistsAsyncMockCalled);
        Assert.Equal(0, result["Result"]);
        Assert.Equal(2, result["Uploaded Count"]);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlockFromDirectory")]
    public async Task UploadBlockFromDirectoryResultTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Data", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "Overwrite", true },
        };
        var result = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments, cts.Token);

        // assert
        Assert.Equal(6, result.Count);
        Assert.True(result.ContainsKey("Result"));
        Assert.Equal(typeof(int), result["Result"]!.GetType());
        Assert.True(result.ContainsKey("Uploaded Count"));
        Assert.Equal(typeof(int), result["Uploaded Count"]!.GetType());
        Assert.True(result.ContainsKey("Azure Error Code"));
        Assert.Equal(typeof(string), result["Azure Error Code"]!.GetType());
        Assert.True(result.ContainsKey("HTTP Status"));
        Assert.Equal(typeof(int), result["HTTP Status"]!.GetType());
        Assert.True(result.ContainsKey("Error Message"));
        Assert.Equal(typeof(string), result["Error Message"]!.GetType());
        Assert.True(result.ContainsKey("Stack Trace"));
        Assert.Equal(typeof(string), result["Stack Trace"]!.GetType());
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlockFromDirectory")]
    public async Task UploadBlockFromDirectoryWithInvalidOptionTest()
    {
        // arrange
        var dirInfo = TestDir.Get("UploadBlockFromDirectory");
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // lack of option
        // act
        var arguments1 = new Dictionary<string, object?>()
        {
            //{ "Directory Path", dirInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result1 = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments1, cts.Token);

        // assert
        Assert.Equal(2, result1["Result"]);
        Assert.Equal("Invalid argument. \"Directory Path\" does not exist.", result1["Error Message"]);
        Assert.NotEqual(string.Empty, result1["Stack Trace"]);
        Assert.Equal(6, result1.Count);

        // act
        var arguments2 = new Dictionary<string, object?>()
        {
            { "Directory Path", dirInfo.FullName },
            //{ "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result2 = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments2, cts.Token);

        // assert
        Assert.Equal(2, result2["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" does not exist.", result2["Error Message"]);
        Assert.NotEqual(string.Empty, result2["Stack Trace"]);
        Assert.Equal(6, result2.Count);

        // act
        var arguments3 = new Dictionary<string, object?>()
        {
            { "Directory Path", dirInfo.FullName },
            { "Container Name", "testContainerName" },
            //{ "Prefix", "hoge" }, <= optional option
            { "Overwrite", true },
        };
        var result3 = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments3, cts.Token);

        // assert
        Assert.Equal(0, result3["Result"]);
        Assert.Equal(string.Empty, result3["Error Message"]);
        Assert.Equal(string.Empty, result3["Stack Trace"]);
        Assert.Equal(6, result3.Count);

        // act
        var arguments4 = new Dictionary<string, object?>()
        {
            { "Directory Path", dirInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            //{ "Overwrite", true }, <= optional option
        };
        var result4 = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments4, cts.Token);

        // assert
        Assert.Equal(0, result4["Result"]);
        Assert.Equal(string.Empty, result4["Error Message"]);
        Assert.Equal(string.Empty, result4["Stack Trace"]);
        Assert.Equal(6, result4.Count);

        // invalid type of option
        // act
        var arguments5 = new Dictionary<string, object?>()
        {
            { "Directory Path", 0 },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result5 = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments5, cts.Token);

        // assert
        Assert.Equal(2, result5["Result"]);
        Assert.Equal("Invalid argument. \"Directory Path\" is not String.", result5["Error Message"]);
        Assert.NotEqual(string.Empty, result5["Stack Trace"]);
        Assert.Equal(6, result5.Count);

        // act
        var arguments6 = new Dictionary<string, object?>()
        {
            { "Directory Path", dirInfo.FullName },
            { "Container Name", 0 },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result6 = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments6, cts.Token);

        // assert
        Assert.Equal(2, result6["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" is not String.", result6["Error Message"]);
        Assert.NotEqual(string.Empty, result6["Stack Trace"]);
        Assert.Equal(6, result6.Count);

        // act
        var arguments7 = new Dictionary<string, object?>()
        {
            { "Directory Path", dirInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", 0 },
            { "Overwrite", true },
        };
        var result7 = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments7, cts.Token);

        // assert
        Assert.Equal(2, result7["Result"]);
        Assert.Equal("Invalid argument. \"Prefix\" is not String.", result7["Error Message"]);
        Assert.NotEqual(string.Empty, result7["Stack Trace"]);
        Assert.Equal(6, result7.Count);

        // act
        var arguments8 = new Dictionary<string, object?>()
        {
            { "Directory Path", dirInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", 0 },
        };
        var result8 = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments8, cts.Token);

        // assert
        Assert.Equal(2, result8["Result"]);
        Assert.Equal("Invalid argument. \"Overwrite\" is not Boolean.", result8["Error Message"]);
        Assert.NotEqual(string.Empty, result8["Stack Trace"]);
        Assert.Equal(6, result8.Count);

        // tear down
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlockFromDirectory")]
    public async Task UploadBlockFromDirectoryErrorFromAzureTest()
    {
        // arrange
        var dirInfo = TestDir.Get("UploadBlockFromDirectory");
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName)
        {
            GetBlobClientMock = (blobName) => new BlobClientMock(blobName)
            {
                UploadAsyncMock = (stream, overwrite, token) => throw new RequestFailedException(status: 1, errorCode: "hoge", message: "fuga", innerException: null)
            }
        });

        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Directory Path", dirInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments, cts.Token);

        // assert
        Assert.Equal(1, result["Result"]);
        Assert.Equal("hoge", result["Azure Error Code"]);
        Assert.Equal(1, result["HTTP Status"]);
        Assert.NotEqual(string.Empty, result["Stack Trace"]);
        Assert.NotNull(result["Stack Trace"]);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.UploadBlockFromDirectory")]
    public async Task UploadBlockFromDirectoryErrorNoDir()
    {
        // arrange
        var dirInfo = TestDir.Get("UploadBlockFromDirectory").Combine("VoidDir");
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName)
        {
            GetBlobClientMock = (blobName) => new BlobClientMock(blobName)
            {
                UploadAsyncMock = (stream, overwrite, token) => throw new RequestFailedException(status: 1, errorCode: "hoge", message: "fuga", innerException: null)
            }
        });

        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Directory Path", dirInfo.FullName },
            { "Container Name", "testContainerName" },
            { "Prefix", "hoge" },
            { "Overwrite", true },
        };
        var result = await controller.ExecuteAsync("UploadBlockFromDirectory", arguments, cts.Token);

        // assert
        Assert.Equal(2, result["Result"]);
        Assert.NotEqual(string.Empty, result["Error Message"]);
        Assert.NotNull(result["Error Message"]);
        Assert.NotEqual(string.Empty, result["Stack Trace"]);
        Assert.NotNull(result["Stack Trace"]);
        await controller.DeleteAsync(cts.Token);
    }
    #endregion

    #region Delete Object
    [Theory]
    [Trait("Category", "BlobStorageController.DeleteObject")]
    [InlineData("testContainerName", "hoge.txt", "eTagValue")]
    [InlineData("testContainerName2", "fuga.txt", null)]
    public async Task DeleteObjectTest(string containerName, string blobPath, string? eTag)
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        string? actualETag = null;
        var actualContainerName = string.Empty;
        var actualBlobPath = string.Empty;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) =>
        {
            actualContainerName = containerName;
            return new BlobContainerClientMock(connectionString, containerName)
            {
                GetBlobClientMock = (blobName) =>
                {
                    actualBlobPath = blobName;
                    return new BlobClientMock(blobName)
                    {
                        DeleteAsyncMock = (option, conditions, token) =>
                        {
                            actualETag = conditions.IfMatch?.ToString();
                            return Task.FromResult((Response)new MockResponse());
                        }
                    };
                }
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Blob Path", blobPath },
            { "Container Name", containerName },
        };
        if (eTag != null)
        {
            arguments.Add("ETag", eTag);
        }
        var result = await controller.ExecuteAsync("DeleteObject", arguments, cts.Token);

        // assert
        Assert.Equal(containerName, actualContainerName);
        Assert.Equal(blobPath, actualBlobPath);
        Assert.Equal(eTag, actualETag);
        Assert.Equal(0, result["Result"]);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.DeleteObject")]
    public async Task DeleteObjectResultTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Blob Path", "hoge.txt" },
            { "Container Name", "containerName" },
            { "ETag", "eTagValue" },
        };
        var result = await controller.ExecuteAsync("DeleteObject", arguments, cts.Token);

        // assert
        Assert.Equal(5, result.Count);
        Assert.True(result.ContainsKey("Result"));
        Assert.Equal(typeof(int), result["Result"]!.GetType());
        Assert.True(result.ContainsKey("Azure Error Code"));
        Assert.Equal(typeof(string), result["Azure Error Code"]!.GetType());
        Assert.True(result.ContainsKey("HTTP Status"));
        Assert.Equal(typeof(int), result["HTTP Status"]!.GetType());
        Assert.True(result.ContainsKey("Error Message"));
        Assert.Equal(typeof(string), result["Error Message"]!.GetType());
        Assert.True(result.ContainsKey("Stack Trace"));
        Assert.Equal(typeof(string), result["Stack Trace"]!.GetType());
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.DeleteObject")]
    public async Task DeleteObjectWithInvalidOptionTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // lack of option
        // act
        var arguments1 = new Dictionary<string, object?>()
        {
            //{ "Blob Path", "hoge.txt" },
            { "Container Name", "containerName" },
            { "ETag", "eTagValue" },
        };
        var result1 = await controller.ExecuteAsync("DeleteObject", arguments1, cts.Token);

        // assert
        Assert.Equal(2, result1["Result"]);
        Assert.Equal("Invalid argument. \"Blob Path\" does not exist.", result1["Error Message"]);
        Assert.NotEqual(string.Empty, result1["Stack Trace"]);
        Assert.Equal(5, result1.Count);

        // act
        var arguments2 = new Dictionary<string, object?>()
        {
            { "Blob Path", "hoge.txt" },
            //{ "Container Name", "containerName" },
            { "ETag", "eTagValue" },
        };
        var result2 = await controller.ExecuteAsync("DeleteObject", arguments2, cts.Token);

        // assert
        Assert.Equal(2, result2["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" does not exist.", result2["Error Message"]);
        Assert.NotEqual(string.Empty, result2["Stack Trace"]);
        Assert.Equal(5, result2.Count);

        // act
        var arguments3 = new Dictionary<string, object?>()
        {
            { "Blob Path", "hoge.txt" },
            { "Container Name", "containerName" },
            //{ "ETag", "eTagValue" }, <= optional option
        };
        var result3 = await controller.ExecuteAsync("DeleteObject", arguments3, cts.Token);

        // assert
        Assert.Equal(0, result3["Result"]);
        Assert.Equal(string.Empty, result3["Error Message"]);
        Assert.Equal(string.Empty, result3["Stack Trace"]);
        Assert.Equal(5, result3.Count);

        // invalid type of option
        // act
        var arguments4 = new Dictionary<string, object?>()
        {
            { "Blob Path", 0 },
            { "Container Name", "containerName" },
            { "ETag", "eTagValue" },
        };
        var result4 = await controller.ExecuteAsync("DeleteObject", arguments4, cts.Token);

        // assert
        Assert.Equal(2, result4["Result"]);
        Assert.Equal("Invalid argument. \"Blob Path\" is not String.", result4["Error Message"]);
        Assert.NotEqual(string.Empty, result4["Stack Trace"]);
        Assert.Equal(5, result4.Count);

        // act
        var arguments5 = new Dictionary<string, object?>()
        {
            { "Blob Path", "hoge.txt" },
            { "Container Name", 0 },
            { "ETag", "eTagValue" },
        };
        var result5 = await controller.ExecuteAsync("DeleteObject", arguments5, cts.Token);

        // assert
        Assert.Equal(2, result5["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" is not String.", result5["Error Message"]);
        Assert.NotEqual(string.Empty, result5["Stack Trace"]);
        Assert.Equal(5, result5.Count);

        // act
        var arguments6 = new Dictionary<string, object?>()
        {
            { "Blob Path", "hoge.txt" },
            { "Container Name", "containerName" },
            { "ETag", 0 },
        };
        var result6 = await controller.ExecuteAsync("DeleteObject", arguments6, cts.Token);

        // assert
        Assert.Equal(2, result6["Result"]);
        Assert.Equal("Invalid argument. \"ETag\" is not String.", result6["Error Message"]);
        Assert.NotEqual(string.Empty, result6["Stack Trace"]);
        Assert.Equal(5, result6.Count);

        // tear down
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.DeleteObject")]
    public async Task DeleteObjectErrorFromAzureTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName)
        {
            GetBlobClientMock = (blobName) => new BlobClientMock(blobName)
            {
                DeleteAsyncMock = (option, condition, token) => throw new RequestFailedException(status: 1, errorCode: "hoge", message: "fuga", innerException: null)
            }
        });

        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Blob Path", "hoge.txt" },
            { "Container Name", "containerName" },
            { "ETag", "eTagValue" },
        };
        var result = await controller.ExecuteAsync("DeleteObject", arguments, cts.Token);

        // assert
        Assert.Equal(1, result["Result"]);
        Assert.Equal("hoge", result["Azure Error Code"]);
        Assert.Equal(1, result["HTTP Status"]);
        Assert.NotEqual(string.Empty, result["Stack Trace"]);
        Assert.NotNull(result["Stack Trace"]);
        await controller.DeleteAsync(cts.Token);
    }
    #endregion

    #region List Objects
    [Theory]
    [Trait("Category", "BlobStorageController.ListObjects")]
    [InlineData("testContainerName", "pre", "pre/")]
    [InlineData("testContainerName", "a/prefix/", "a/prefix/")]
    [InlineData("testContainerName", "a/b/prefix", "a/b/prefix/")]
    [InlineData("testContainerName2", null, null)]
    public async Task ListObjectsTest(string containerName, string? prefix, string? expectedPrefix)
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var actualContainerName = string.Empty;
        string? actualPrefix = null;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) =>
        {
            actualContainerName = containerName;
            return new BlobContainerClientMock(connectionString, containerName)
            {
                GetBlobsAsyncMock = (prefix, token) =>
                {
                    actualPrefix = prefix;
                    return new AsyncPageableBlobItemMock();
                }
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Container Name", containerName },
        };
        if (prefix != null)
        {
            arguments.Add("Prefix", prefix);
        }
        var result = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);

        // assert
        Assert.Equal(containerName, actualContainerName);
        Assert.Equal(expectedPrefix, actualPrefix);
        Assert.Equal(0, result["Result"]);
        Assert.Equal(2, result["Object Count"]);
        Assert.Equal("test1.txt", ((string[])result["Object Names"]!)[0]);
        Assert.Equal("test2.jpg", ((string[])result["Object Names"]!)[1]);
        Assert.Equal(2, ((string[])result["Object Names"]!).Length);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.ListObjects")]
    public async Task ListObjectsResultTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Blob Path", "hoge.txt" },
            { "Container Name", "containerName" },
            { "ETag", "eTagValue" },
        };
        var result = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);

        // assert
        Assert.Equal(7, result.Count);
        Assert.True(result.ContainsKey("Result"));
        Assert.Equal(typeof(int), result["Result"]!.GetType());
        Assert.True(result.ContainsKey("Object Count"));
        Assert.Equal(typeof(int), result["Object Count"]!.GetType());
        Assert.True(result.ContainsKey("Object Names"));
        Assert.Equal(typeof(string[]), result["Object Names"]!.GetType());
        Assert.True(result.ContainsKey("Azure Error Code"));
        Assert.Equal(typeof(string), result["Azure Error Code"]!.GetType());
        Assert.True(result.ContainsKey("HTTP Status"));
        Assert.Equal(typeof(int), result["HTTP Status"]!.GetType());
        Assert.True(result.ContainsKey("Error Message"));
        Assert.Equal(typeof(string), result["Error Message"]!.GetType());
        Assert.True(result.ContainsKey("Stack Trace"));
        Assert.Equal(typeof(string), result["Stack Trace"]!.GetType());
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.ListObjects")]
    public async Task ListObjectsWithInvalidOptionTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // lack of option
        // act
        var arguments1 = new Dictionary<string, object?>()
        {
            //{ "Container Name", "containerName" },
            { "Prefix", "prefix" },
        };
        var result1 = await controller.ExecuteAsync("ListObjects", arguments1, cts.Token);

        // assert
        Assert.Equal(2, result1["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" does not exist.", result1["Error Message"]);
        Assert.NotEqual(string.Empty, result1["Stack Trace"]);
        Assert.Equal(7, result1.Count);

        // act
        var arguments2 = new Dictionary<string, object?>()
        {
            { "Container Name", "containerName" },
            //{ "Prefix", "prefix" }, <= optional option
        };
        var result2 = await controller.ExecuteAsync("ListObjects", arguments2, cts.Token);

        // assert
        Assert.Equal(0, result2["Result"]);
        Assert.Equal(string.Empty, result2["Error Message"]);
        Assert.Equal(string.Empty, result2["Stack Trace"]);
        Assert.Equal(7, result2.Count);

        // invalid type of option
        // act
        var arguments3 = new Dictionary<string, object?>()
        {
            { "Container Name", 0 },
            { "Prefix", "prefix" },
        };
        var result3 = await controller.ExecuteAsync("ListObjects", arguments3, cts.Token);

        // assert
        Assert.Equal(2, result3["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" is not String.", result3["Error Message"]);
        Assert.NotEqual(string.Empty, result3["Stack Trace"]);
        Assert.Equal(7, result3.Count);

        // act
        var arguments4 = new Dictionary<string, object?>()
        {
            { "Container Name", "containerName" },
            { "Prefix", 0 },
        };
        var result4 = await controller.ExecuteAsync("ListObjects", arguments4, cts.Token);

        // assert
        Assert.Equal(2, result4["Result"]);
        Assert.Equal("Invalid argument. \"Prefix\" is not String.", result4["Error Message"]);
        Assert.NotEqual(string.Empty, result4["Stack Trace"]);
        Assert.Equal(7, result4.Count);

        // tear down
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.ListObjects")]
    public async Task ListObjectsErrorFromAzureTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName)
        {
            GetBlobsAsyncMock = (prerix, token) => throw new RequestFailedException(status: 1, errorCode: "hoge", message: "fuga", innerException: null)
        });

        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Container Name", "containerName" },
            { "Prefix", "prefix" },
        };
        var result = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);

        // assert
        Assert.Equal(1, result["Result"]);
        Assert.Equal("hoge", result["Azure Error Code"]);
        Assert.Equal(1, result["HTTP Status"]);
        Assert.NotEqual(string.Empty, result["Stack Trace"]);
        Assert.NotNull(result["Stack Trace"]);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.ListObjects")]
    public async Task ListObjectsTooMuchFilesTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName)
        {
            GetBlobsAsyncMock = (prerix, token) => new AsyncPageableBlobItemMock(201)
        });

        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Container Name", "containerName" },
            { "Prefix", "prefix" },
        };
        var result = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);

        // assert
        Assert.Equal(0, result["Result"]);
        Assert.Equal(201, result["Object Count"]);
        Assert.Equal("1.txt", ((string[])result["Object Names"]!)[0]);
        Assert.Equal("2.txt", ((string[])result["Object Names"]!)[1]);
        Assert.Equal(201, ((string[])result["Object Names"]!).Length);
        await controller.DeleteAsync(cts.Token);
    }
    #endregion

    #region Upload Block
    [Theory]
    [Trait("Category", "BlobStorageController.Append")]
    [InlineData("testContainerName", "abc.txt", new byte[] { 1, 2, 3 }, "eTagValue")]
    [InlineData("testContainerName2", "xyz.txt", new byte[0], "eTagValue2")]
    public async Task AppendTest(string containerName, string blobPath, byte[] data, string eTag)
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var createIfNotExistsAsyncMockCalled = false;
        var createIfNotExistsAsyncMockCalled2 = false;
        var now = DateTime.Now;
        var actualContainerName = string.Empty;
        var actualBlobPath = string.Empty;
        var actualData = Array.Empty<byte>();
        var actualETag = string.Empty;
        var actualETag2 = string.Empty;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) =>
        {
            actualContainerName = containerName;
            return new BlobContainerClientMock(connectionString, containerName)
            {
                CreateIfNotExistsAsyncMock = (token) =>
                {
                    createIfNotExistsAsyncMockCalled = true;
                    var info = BlobsModelFactory.BlobContainerInfo(
                        lastModified: new DateTimeOffset(DateTime.Now),
                        eTag: new ETag(Guid.NewGuid().ToString())
                    );
                    return Task.FromResult(Response.FromValue(info, null!));
                },
                GetAppendBlobClientMock = (blobName) =>
                {
                    actualBlobPath = blobName;
                    return new AppendBlobClientMock(blobName)
                    {
                        CreateIfNotExistsAsyncMock = (token) =>
                        {
                            createIfNotExistsAsyncMockCalled2 = true;
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
                        },
                        AppendBlockAsyncMock = (stream, option, token) =>
                        {
                            actualData = stream.ToArray();
                            actualETag = option.Conditions.IfMatch.ToString();
                            actualETag2 = new ETag(Guid.NewGuid().ToString()).ToString();
                            var info = BlobsModelFactory.BlobAppendInfo(
                                lastModified: new DateTimeOffset(now),
                                eTag: new ETag(actualETag2),
                                contentHash: [],
                                contentCrc64: [],
                                blobAppendOffset: string.Empty,
                                blobCommittedBlockCount: 0,
                                isServerEncrypted: false,
                                encryptionKeySha256: string.Empty,
                                encryptionScope: string.Empty
                            );
                            return Task.FromResult(Response.FromValue(info, null!));
                        },
                    };
                },
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bytes", data },
            { "Container Name", containerName },
            { "Blob Path", blobPath },
            { "ETag", eTag },
        };
        var result = await controller.ExecuteAsync("Append", arguments, cts.Token);

        // assert
        Assert.Equal(containerName, actualContainerName);
        Assert.Equal(blobPath, actualBlobPath);
        Assert.Equal(data, actualData);
        Assert.True(createIfNotExistsAsyncMockCalled);
        Assert.True(createIfNotExistsAsyncMockCalled2);
        Assert.Equal(0, result["Result"]);
        Assert.Equal(blobPath, result["Blob Path"]);
        Assert.Equal($"http://hoge.com/{blobPath}", result["Uri"]);
        Assert.Equal(eTag.ToString(), actualETag); 
        Assert.Equal(actualETag2, result["ETag"]);
        Assert.Equal(now.ToUniversalTime(), result["Last Modified"]);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.Append")]
    public async Task AppendResultTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "ETag", "eTagValue" },
        };
        var result = await controller.ExecuteAsync("Append", arguments, cts.Token);

        // assert
        Assert.Equal(9, result.Count);
        Assert.True(result.ContainsKey("Result"));
        Assert.Equal(typeof(int), result["Result"]!.GetType());
        Assert.True(result.ContainsKey("Blob Path"));
        Assert.Equal(typeof(string), result["Blob Path"]!.GetType());
        Assert.True(result.ContainsKey("Uri"));
        Assert.Equal(typeof(string), result["Uri"]!.GetType());
        Assert.True(result.ContainsKey("ETag"));
        Assert.Equal(typeof(string), result["ETag"]!.GetType());
        Assert.True(result.ContainsKey("Last Modified"));
        Assert.Equal(typeof(DateTime), result["Last Modified"]!.GetType());
        Assert.True(result.ContainsKey("Azure Error Code"));
        Assert.Equal(typeof(string), result["Azure Error Code"]!.GetType());
        Assert.True(result.ContainsKey("HTTP Status"));
        Assert.Equal(typeof(int), result["HTTP Status"]!.GetType());
        Assert.True(result.ContainsKey("Error Message"));
        Assert.Equal(typeof(string), result["Error Message"]!.GetType());
        Assert.True(result.ContainsKey("Stack Trace"));
        Assert.Equal(typeof(string), result["Stack Trace"]!.GetType());
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.Append")]
    public async Task AppendWithInvalidOptionTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName));
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // lack of option
        // act
        var arguments1 = new Dictionary<string, object?>()
        {
            //{ "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "ETag", "eTagValue" },
        };
        var result1 = await controller.ExecuteAsync("Append", arguments1, cts.Token);

        // assert
        Assert.Equal(2, result1["Result"]);
        Assert.Equal("Invalid argument. \"Bytes\" does not exist.", result1["Error Message"]);
        Assert.NotEqual(string.Empty, result1["Stack Trace"]);
        Assert.Equal(9, result1.Count);

        // act
        var arguments2 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            //{ "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "ETag", "eTagValue" },
        };
        var result2 = await controller.ExecuteAsync("Append", arguments2, cts.Token);

        // assert
        Assert.Equal(2, result2["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" does not exist.", result2["Error Message"]);
        Assert.NotEqual(string.Empty, result2["Stack Trace"]);
        Assert.Equal(9, result2.Count);

        // act
        var arguments3 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            //{ "Blob Path", "abc.txt" },
            { "ETag", "eTagValue" },
        };
        var result3 = await controller.ExecuteAsync("Append", arguments3, cts.Token);

        // assert
        Assert.Equal(2, result3["Result"]);
        Assert.Equal("Invalid argument. \"Blob Path\" does not exist.", result3["Error Message"]);
        Assert.NotEqual(string.Empty, result3["Stack Trace"]);
        Assert.Equal(9, result3.Count);

        // act
        var arguments4 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            //{ "ETag", "eTagValue" }, <= optional option
        };
        var result4 = await controller.ExecuteAsync("Append", arguments4, cts.Token);

        // assert
        Assert.Equal(0, result4["Result"]);
        Assert.Equal(string.Empty, result4["Error Message"]);
        Assert.Equal(string.Empty, result4["Stack Trace"]);
        Assert.Equal(9, result4.Count);

        // invalid type of option
        // act
        var arguments5 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<int>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "ETag", "eTagValue" },
        };
        var result5 = await controller.ExecuteAsync("Append", arguments5, cts.Token);

        // assert
        Assert.Equal(2, result5["Result"]);
        Assert.Equal("Invalid argument. \"Bytes\" is not Byte[].", result5["Error Message"]);
        Assert.NotEqual(string.Empty, result5["Stack Trace"]);
        Assert.Equal(9, result5.Count);

        // act
        var arguments6 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", 0 },
            { "Blob Path", "abc.txt" },
            { "ETag", "eTagValue" },
        };
        var result6 = await controller.ExecuteAsync("Append", arguments6, cts.Token);

        // assert
        Assert.Equal(2, result6["Result"]);
        Assert.Equal("Invalid argument. \"Container Name\" is not String.", result6["Error Message"]);
        Assert.NotEqual(string.Empty, result6["Stack Trace"]);
        Assert.Equal(9, result6.Count);

        // act
        var arguments7 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", 0 },
            { "ETag", "eTagValue" },
        };
        var result7 = await controller.ExecuteAsync("Append", arguments7, cts.Token);

        // assert
        Assert.Equal(2, result7["Result"]);
        Assert.Equal("Invalid argument. \"Blob Path\" is not String.", result7["Error Message"]);
        Assert.NotEqual(string.Empty, result7["Stack Trace"]);
        Assert.Equal(9, result7.Count);

        // act
        var arguments8 = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "testContainerName" },
            { "Blob Path", "abc.txt" },
            { "ETag", 0 },
        };
        var result8 = await controller.ExecuteAsync("Append", arguments8, cts.Token);

        // assert
        Assert.Equal(2, result8["Result"]);
        Assert.Equal("Invalid argument. \"ETag\" is not String.", result8["Error Message"]);
        Assert.NotEqual(string.Empty, result8["Stack Trace"]);
        Assert.Equal(9, result8.Count);

        // tear down
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "BlobStorageController.Append")]
    public async Task AppendErrorFromAzureTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, containerName) => new BlobContainerClientMock(connectionString, containerName)
        {
            GetAppendBlobClientMock = (blobName) => new AppendBlobClientMock(blobName)
            {
                AppendBlockAsyncMock = (stream, overwrite, token) => throw new RequestFailedException(status: 1, errorCode: "hoge", message: "fuga", innerException: null)
            }
        });

        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\"}",
            token: cts.Token);

        // act
        var arguments = new Dictionary<string, object?>()
        {
            { "Bytes", Array.Empty<byte>() },
            { "Container Name", "containerName" },
            { "Blob Path", "abc.txt" },
            { "ETag", "eTagValue" },
        };
        var result = await controller.ExecuteAsync("Append", arguments, cts.Token);

        // assert
        Assert.Equal(1, result["Result"]);
        Assert.Equal("hoge", result["Azure Error Code"]);
        Assert.Equal(1, result["HTTP Status"]);
        Assert.NotEqual(string.Empty, result["Stack Trace"]);
        Assert.NotNull(result["Stack Trace"]);
        await controller.DeleteAsync(cts.Token);
    }
    #endregion
}
