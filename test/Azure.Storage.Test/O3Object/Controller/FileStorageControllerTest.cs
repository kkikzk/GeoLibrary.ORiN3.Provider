using Design.ORiN3.Provider.V1;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;
using GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;
using GeoLibrary.ORiN3.Provider.TestBaseLib;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.O3Object.Controller;

public class FileStorageControllerTest : IClassFixture<ProviderTestFixture<FileStorageControllerTest>>, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ProviderTestFixture<FileStorageControllerTest> _fixture;
    private readonly CancellationTokenSource _tokenSource = new(10000);

    public FileStorageControllerTest(ProviderTestFixture<FileStorageControllerTest> fixture, ITestOutputHelper output)
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
    [Trait("Category", nameof(FileStorageController))]
    [InlineData("iotsolution1", "YMktKCsVW7tZrnFKLqFRD8MRICu3hNXxaNTB9Ejr/XyTnM30Eimpy6JvHDNWubcj", "true", "core.windows.net", "DefaultEndpointsProtocol=https;AccountName=iotsolution1;AccountKey=testKey;EndpointSuffix=core.windows.net")]
    [InlineData("hogefuga", "CZnsEFFRZO7dX9nuWVXVo9H5MSs778fhxZRKUTRiSvdg7jMy8xzWVlrlDuXH6tq2", "false", "hoge.net", "DefaultEndpointsProtocol=http;AccountName=hogefuga;AccountKey=testKey2;EndpointSuffix=hoge.net")]
    public async Task ControllerOptionTest(string accountName, string accountKey, string useHttps, string endpointSuffix, string expected)
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var actualConnectionString = string.Empty;
        using var methodReverter = ShareClientEx.SetCreateMethod((connectionString, proxyUri, shareName) =>
        {
            actualConnectionString = connectionString;
            return new ShareClientMock(connectionString, shareName);
        });

        // act
        var sut = await _fixture.Root.CreateControllerAsync(
            name: "AzureFileStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.FileStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Account Name\":\"" + accountName + "\",\"Account Key\":\"" + accountKey + "\",\"Use Https\":" + useHttps + ",\"Endpoint Suffix\":\"" + endpointSuffix + "\"}",
            token: cts.Token);
        var file = await sut.CreateFileAsync(
            name: "BlobFile",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.AzureFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Share Name\":\"test\",\"File Name\":\"hoge.txt\"}",
            token: cts.Token);
        await file.OpenAsync();

        // assert
        Assert.Equal(expected, actualConnectionString);
        await sut.DeleteAsync(cts.Token);
    }

    [Theory]
    [Trait("Category", nameof(FileStorageController))]
    [InlineData("{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"c6OZIVO5OxjhO81qy8MEhCE6haxaNSIugpW/9QEQmfAK2ylTZ\\u002B1fAidpEG0nrnUd\"}", "DefaultEndpointsProtocol=https;AccountName=iotsolution1;AccountKey=testKey;EndpointSuffix=core.windows.net")]
    [InlineData("{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"c6OZIVO5OxjhO81qy8MEhCE6haxaNSIugpW/9QEQmfAK2ylTZ\\u002B1fAidpEG0nrnUd\",\"Use Https\":true}", "DefaultEndpointsProtocol=https;AccountName=iotsolution1;AccountKey=testKey;EndpointSuffix=core.windows.net")]
    [InlineData("{\"@Version\":\"0.0.1\",\"Account Name\":\"iotsolution1\",\"Account Key\":\"c6OZIVO5OxjhO81qy8MEhCE6haxaNSIugpW/9QEQmfAK2ylTZ\\u002B1fAidpEG0nrnUd\",\"Endpoint Suffix\":\"core.windows.net\"}", "DefaultEndpointsProtocol=https;AccountName=iotsolution1;AccountKey=testKey;EndpointSuffix=core.windows.net")]
    public async Task OptionalOptionTest(string option, string expected)
    {
        // arrange
        using var cts = new CancellationTokenSource(10000);
        var actualConnectionString = string.Empty;
        using var methodReverter = ShareClientEx.SetCreateMethod((connectionString, proxyUri, shareName) =>
        {
            actualConnectionString = connectionString;
            return new ShareClientMock(connectionString, shareName);
        });

        // act
        var sut = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.FileStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: option,
            token: cts.Token);
        var file = await sut.CreateFileAsync(
            name: "BlobFile",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.AzureFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"0.0.1\",\"Share Name\":\"test\",\"File Name\":\"hoge.txt\"}",
            token: cts.Token);
        await file.OpenAsync();

        // assert
        Assert.Equal(expected, actualConnectionString);
        await sut.DeleteAsync(cts.Token);
    }
}
