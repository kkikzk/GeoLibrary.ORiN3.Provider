using Azure;
using Azure.Storage.Blobs.Models;
using Design.ORiN3.Provider.V1;
using Design.ORiN3.Provider.V1.Base;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;
using GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;
using GeoLibrary.ORiN3.Provider.BaseLib;
using GeoLibrary.ORiN3.Provider.TestBaseLib;
using Message.Client.ORiN3.Provider;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.O3Object.File;

public class PageBlobFileTest : IClassFixture<ProviderTestFixture<PageBlobFileTest>>, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ProviderTestFixture<PageBlobFileTest> _fixture;
    private readonly CancellationTokenSource _tokenSource = new(10000);

    public PageBlobFileTest(ProviderTestFixture<PageBlobFileTest> fixture, ITestOutputHelper output)
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

    [Theory]
    [Trait("Category", nameof(PageBlobFile))]
    [InlineData("containerName", "hoge.txt")]
    [InlineData("containerName2", "a/b/c/fuga.txt")]
    public async Task FileOptionTest(string containerName, string blobPath)
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var actualConnectionString = string.Empty;
        var actualcontainerName = string.Empty;
        var actualBlobPath = string.Empty;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, proxyUri, containerName) =>
        {
            actualConnectionString = connectionString;
            actualcontainerName = containerName;
            return new BlobContainerClientMock(connectionString, containerName)
            {
                GetPageBlobClientMock = (blobName) =>
                {
                    actualBlobPath = blobName;
                    return new PageBlobClientMock(blobName);
                },
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\",\"Use Https\":true,\"Endpoint Suffix\":\"core.windows.net\"}",
            token: cts.Token);

        // act
        var sut = await controller.CreateFileAsync(
            name: "BlobFile",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Container Name\":\"" + containerName + "\",\"Blob Path\":\"" + blobPath + "\"}",
            token: cts.Token);
        await sut.OpenAsync(cts.Token);

        // assert
        Assert.Equal(containerName, actualcontainerName);
        Assert.Equal(blobPath, actualBlobPath);
        Assert.Equal("DefaultEndpointsProtocol=https;AccountName=iotsolution1;AccountKey=testKey;EndpointSuffix=core.windows.net", actualConnectionString);

        await sut.CloseAsync(cts.Token);
        await sut.DeleteAsync(cts.Token);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", nameof(PageBlobFile))]
    public async Task FileOptionCreateIfNotExistsTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var actualLength = 0L;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, proxyUri, containerName) =>
        {
            return new BlobContainerClientMock(connectionString, containerName)
            {
                GetPageBlobClientMock = (blobName) =>
                {
                    return new PageBlobClientMock(blobName, false)
                    {
                        CreateAsyncMock = (length, token) =>
                        {
                            actualLength = length;
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
                    };
                },
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\",\"Use Https\":true,\"Endpoint Suffix\":\"core.windows.net\"}",
            token: cts.Token);

        // act
        var sut = await controller.CreateFileAsync(
            name: "BlobFile",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Container Name\":\"containerName\",\"Blob Path\":\"hoge.txt\",\"Create New If Not Exists\":true,\"Length\":100}",
            token: cts.Token);
        await sut.OpenAsync(cts.Token);

        // assert
        Assert.Equal(100L, actualLength);

        await sut.CloseAsync(cts.Token);
        await sut.DeleteAsync(cts.Token);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", nameof(PageBlobFile))]
    public async Task FileOptionCreateIfNotExistsErrorTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var called = false;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, proxyUri, containerName) =>
        {
            return new BlobContainerClientMock(connectionString, containerName)
            {
                GetPageBlobClientMock = (blobName) =>
                {
                    return new PageBlobClientMock(blobName, true)
                    {
                        CreateAsyncMock = (length, token) =>
                        {
                            called = true;
                            throw new Exception();
                        },
                    };
                },
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\",\"Use Https\":true,\"Endpoint Suffix\":\"core.windows.net\"}",
            token: cts.Token);

        // act
        var sut = await controller.CreateFileAsync(
            name: "BlobFile",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Container Name\":\"containerName\",\"Blob Path\":\"hoge.txt\",\"Create New If Not Exists\":true,\"Length\":100}",
            token: cts.Token);
        await sut.OpenAsync(cts.Token);

        // assert
        Assert.False(called);

        await sut.CloseAsync(cts.Token);
        await sut.DeleteAsync(cts.Token);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", "PageBlobFile")]
    public async Task FileOptionCreateIfNotExistsErrorTest2()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var called = false;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, proxyUri, containerName) =>
        {
            return new BlobContainerClientMock(connectionString, containerName)
            {
                GetPageBlobClientMock = (blobName) =>
                {
                    return new PageBlobClientMock(blobName, false)
                    {
                        CreateAsyncMock = (length, token) =>
                        {
                            called = true;
                            throw new Exception();
                        },
                    };
                },
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\",\"Use Https\":true,\"Endpoint Suffix\":\"core.windows.net\"}",
            token: cts.Token);

        // act
        var sut = await controller.CreateFileAsync(
            name: "BlobFile",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Container Name\":\"containerName\",\"Blob Path\":\"hoge.txt\",\"Create New If Not Exists\":false,\"Length\":100}",
            token: cts.Token);

        // assert
        var exception = await Assert.ThrowsAsync<ProviderClientException>(async () =>
        {
            await sut.OpenAsync(cts.Token);
        });
        Assert.False(called);

        await sut.CloseAsync(cts.Token);
        await sut.DeleteAsync(cts.Token);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", nameof(PageBlobFile))]
    public async Task FileOptionCreateIfNotExistsErrorTest3()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var called = false;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, proxyUri, containerName) =>
        {
            return new BlobContainerClientMock(connectionString, containerName)
            {
                GetPageBlobClientMock = (blobName) =>
                {
                    return new PageBlobClientMock(blobName, false)
                    {
                        CreateAsyncMock = (length, token) =>
                        {
                            called = true;
                            throw new Exception();
                        },
                    };
                },
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\",\"Use Https\":true,\"Endpoint Suffix\":\"core.windows.net\"}",
            token: cts.Token);

        // act
        var sut = await controller.CreateFileAsync(
            name: "BlobFile",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Container Name\":\"containerName\",\"Blob Path\":\"hoge.txt\",\"Create New If Not Exists\":true}",
            token: cts.Token);

        // assert
        var exception = await Assert.ThrowsAsync<ProviderClientException>(async () =>
        {
            await sut.OpenAsync(cts.Token);
        });
        Assert.False(called);

        await sut.CloseAsync(cts.Token);
        await sut.DeleteAsync(cts.Token);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", nameof(PageBlobFile))]
    public async Task FileOptionCreateIfNotExistsErrorTest4()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var called = false;
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, proxyUri, containerName) =>
        {
            return new BlobContainerClientMock(connectionString, containerName)
            {
                GetPageBlobClientMock = (blobName) =>
                {
                    return new PageBlobClientMock(blobName, false)
                    {
                        CreateAsyncMock = (length, token) =>
                        {
                            called = true;
                            throw new Exception();
                        },
                    };
                },
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\",\"Use Https\":true,\"Endpoint Suffix\":\"core.windows.net\"}",
            token: cts.Token);

        // act
        // assert
        var exception = await Assert.ThrowsAsync<ProviderClientException>(async () =>
        {
            _ = await controller.CreateFileAsync(
                name: "BlobFile",
                typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
                option: "{\"@Version\":\"0.0.1\",\"Container Name\":\"containerName\",\"Blob Path\":\"hoge.txt\",\"Create New If Not Exists\":true,\"Length\":-1}",
                token: cts.Token);
        });
        Assert.False(called);

        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", nameof(PageBlobFile))]
    public async Task FileOptionCreateIfNotExistsErrorTest5()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, proxyUri, containerName) =>
        {
            return new BlobContainerClientMock(connectionString, containerName)
            {
                GetPageBlobClientMock = (blobName) =>
                {
                    return new PageBlobClientMock(blobName, false);
                },
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\",\"Use Https\":true,\"Endpoint Suffix\":\"core.windows.net\"}",
            token: cts.Token);

        // act
        var sut = await controller.CreateFileAsync(
            name: "BlobFile",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Container Name\":\"containerName\",\"Blob Path\":\"hoge.txt\",\"Create New If Not Exists\":true,\"Length\":10}",
            token: cts.Token);

        // assert
        var exception = await Assert.ThrowsAsync<ProviderClientException>(async () =>
        {
            await sut.OpenAsync(cts.Token);
        });

        await sut.CloseAsync(cts.Token);
        await sut.DeleteAsync(cts.Token);
        await controller.DeleteAsync(cts.Token);
    }

    [Fact]
    [Trait("Category", nameof(PageBlobFile))]
    public async Task FileWriteAndReadTest()
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        using var methodReverter = BlobContainerClientEx.SetCreateMethod((connectionString, proxyUri, containerName) =>
        {
            return new BlobContainerClientMock(connectionString, containerName)
            {
                GetPageBlobClientMock = (blobName) =>
                {
                    return new PageBlobClientMock(blobName, false);
                },
            };
        });
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj\",\"Use Https\":true,\"Endpoint Suffix\":\"core.windows.net\"}",
            token: cts.Token);

        // act
        var sut = await controller.CreateFileAsync(
            name: "BlobFile",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Container Name\":\"containerName\",\"Blob Path\":\"hoge.txt\",\"Create New If Not Exists\":true,\"Length\":1024}",
            token: cts.Token);
        await sut.OpenAsync(cts.Token);
        await sut.SeekAsync(511, ORiN3FileSeekOrigin.Begin, cts.Token);
        await sut.WriteAsync(new byte[] { 1, 2, 3 }, cts.Token);
        await sut.SeekAsync(0, ORiN3FileSeekOrigin.Begin, cts.Token);
        var buffer = new byte[1024];
        await sut.ReadAsync(buffer, cts.Token);

        Assert.True(buffer.Take(511).All(_ => _ == 0));
        Assert.True(buffer.Skip(515).All(_ => _ == 0));
        Assert.Equal(1, buffer[511]);
        Assert.Equal(2, buffer[512]);
        Assert.Equal(3, buffer[513]);

        await sut.CloseAsync(cts.Token);
        await sut.DeleteAsync(cts.Token);
        await controller.DeleteAsync(cts.Token);
    }

    [Theory]
    [Trait("Category", nameof(PageCalculator))]
    [InlineData(0L, 512, 0L, 511L, 512)]
    [InlineData(0L, 1024, 0L, 1023L, 1024)]
    [InlineData(0L, 1536, 0L, 1535L, 1536)]
    [InlineData(512L, 512, 512L, 1023L, 512)]
    [InlineData(1024L, 512, 1024L, 1535, 512)]
    [InlineData(0, 1, 0L, 511L, 512)]
    [InlineData(1, 1, 0L, 511L, 512)]
    [InlineData(510, 1, 0L, 511L, 512)]
    [InlineData(511, 1, 0L, 511L, 512)]
    [InlineData(512, 1, 512L, 1023L, 512)]
    [InlineData(513, 1, 512L, 1023L, 512)]
    [InlineData(1022, 1, 512L, 1023L, 512)]
    [InlineData(1023, 1, 512L, 1023L, 512)]
    [InlineData(1024, 1, 1024L, 1535L, 512)]
    [InlineData(0, 2, 0L, 511L, 512)]
    [InlineData(510, 2, 0L, 511L, 512)]
    [InlineData(511, 2, 0L, 1023L, 1024)]
    [InlineData(512, 2, 512L, 1023L, 512)]
    [InlineData(1022, 2, 512L, 1023L, 512)]
    [InlineData(1023, 2, 512L, 1535L, 1024)]
    [InlineData(1024, 2, 1024L, 1535L, 512)]
    [InlineData(0, 1024, 0L, 1023L, 1024)]
    [InlineData(0, 1025, 0L, 1535L, 1536)]
    [InlineData(511, 513, 0L, 1023L, 1024)]
    [InlineData(511, 514, 0L, 1535L, 1536)]
    [InlineData(1023, 514, 512L, 2047L, 1536)]
    public void PageCalculatorTest(long dataPosition, int dataLength, long expectedPageFirstIndex, long expectedPageLastIndex, int expectedLength)
    {
        _output.WriteLine($"{nameof(dataPosition)}={dataPosition}, {nameof(dataLength)}={dataLength}, {nameof(expectedPageFirstIndex)}={expectedPageFirstIndex}, {nameof(expectedPageLastIndex)}={expectedPageLastIndex}, {nameof(expectedLength)}={expectedLength}");
        var sut = new PageCalculator(dataPosition, dataLength);

        _output.WriteLine($"{nameof(sut.PageFirstIndex)}={sut.PageFirstIndex}, {nameof(sut.PageLastIndex)}={sut.PageLastIndex}, {nameof(sut.Length)}={sut.Length}");
        Assert.Equal(expectedPageFirstIndex, sut.PageFirstIndex);
        Assert.Equal(expectedPageLastIndex, sut.PageLastIndex);
        Assert.Equal(expectedLength, sut.Length);
    }

    [Theory]
    [Trait("Category", nameof(PageCalculator))]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageCalculatorDataLengthErrorTest(int dataLength)
    {
        var exception = Assert.Throws<GeoLibraryProviderException>(() =>
        {
            _ = new PageCalculator(0, dataLength);
        });

        Assert.Equal((int)GeoLibraryProviderResultCode.Unknown, (int)exception.ResultCode);
        Assert.Contains("dataLength", exception.Message);
    }

    [Theory]
    [Trait("Category", nameof(PageCalculator))]
    [InlineData(-1)]
    public void PageCalculatorDataPositionErrorTest(int dataPosition)
    {
        var exception = Assert.Throws<GeoLibraryProviderException>(() =>
        {
            _ = new PageCalculator(dataPosition, 1);
        });

        Assert.Equal((int)GeoLibraryProviderResultCode.Unknown, (int)exception.ResultCode);
        Assert.Contains("dataPosition", exception.Message);
    }

    [Theory]
    [Trait("Category", nameof(PageCalculator))]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageCalculatorPageSizeErrorTest(long pageSize)
    {
        var exception = Assert.Throws<GeoLibraryProviderException>(() =>
        {
            _ = new PageCalculator(0, 1, pageSize);
        });

        Assert.Equal((int)GeoLibraryProviderResultCode.Unknown, (int)exception.ResultCode);
        Assert.Contains("pageSize", exception.Message);
    }
}
