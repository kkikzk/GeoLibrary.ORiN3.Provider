using Design.ORiN3.Provider.V1;
using Design.ORiN3.Provider.V1.Base;
using GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;
using GeoLibrary.ORiN3.Provider.BaseLib;
using GeoLibrary.ORiN3.Provider.TestBaseLib;
using Message.Client.ORiN3.Provider;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test;

public class ManualTest : IClassFixture<ProviderTestFixture<ManualTest>>, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ProviderTestFixture<ManualTest> _fixture;
    private readonly CancellationTokenSource _tokenSource = new(10000);

    public ManualTest(ProviderTestFixture<ManualTest> fixture, ITestOutputHelper output)
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

    // [Fact]
    public async Task Test()
    {
        var version = "0.0.1";
        var accountName = "***";
        var accessKey = "***";
        var containerName = "***";

        Dictionary<string, object?> makeArg()
        {
            var result = new Dictionary<string, object?>
            {
                ["Container Name"] = containerName
            };
            return result;
        }
        Dictionary<string, object?> makeArgs(Tuple<string, object?>[] values)
        {
            var result = makeArg();
            foreach (var it in values)
            {
                result[it.Item1] = it.Item2;
            }
            return result;
        }
        async Task<string[]> list(IController controller)
        {
            var result = await controller.ExecuteAsync("ListObjects", makeArg());
            Assert.Equal(0, result["Result"]);
            _output.WriteLine($"Object count={result["Object Count"]}");
            return (string[])result["Object Names"]!;
        }
        async Task delete(IController controller, string blob)
        {
            var result = await controller.ExecuteAsync("DeleteObject", makeArgs([Tuple.Create("Blob Path", (object?)blob)]));
            Assert.Equal(0, result["Result"]);
            _output.WriteLine($"Object deleted. [path={blob}]");
        }
        async Task<IFile> createBlock(IController controller, string blob)
        {
            return await controller.CreateFileAsync(
                name: "BlobFile",
                typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.BlockBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
                option: "{\"@Version\":\"" + version + "\",\"Container Name\":\"" + containerName + "\",\"Blob Path\":\"" + blob + "\"}");
        }
        async Task<IFile> createAppend(IController controller, string blob)
        {
            return await controller.CreateFileAsync(
                name: "BlobFile",
                typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.AppendBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
                option: "{\"@Version\":\"" + version + "\",\"Container Name\":\"" + containerName + "\",\"Blob Path\":\"" + blob + "\"}");
        }
        async Task writeAndReadData(IController controller, int startIndex, int dataSize)
        {
            var blobName = Guid.NewGuid().ToString();
            var file = await controller.CreateFileAsync(
                name: "BlobFile",
                typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
                option: "{\"@Version\":\"" + version + "\",\"Container Name\":\"" + containerName + "\",\"Blob Path\":\"" + blobName + "\",\"Create New If Not Exists\":true,\"Length\":1536}");
            await file.OpenAsync();
            var length = await file.GetLengthAsync();
            Assert.Equal(1536, length);

            _output.WriteLine($"writeAndReadData => startIndex={startIndex}, dataSize={dataSize}");
            var data = new byte[dataSize];
            for (var i = 0; i < data.Length; ++i)
            {
                data[i] = 255;
            }
            await file.SeekAsync(startIndex, ORiN3FileSeekOrigin.Begin);
            await file.WriteAsync(data);
            await file.SeekAsync(0, ORiN3FileSeekOrigin.Begin);
            var buffer = new byte[1536];
            var length2 = await file.ReadAsync(buffer);
            Assert.Equal(1536, length2);
            var first = buffer.AsMemory()[..startIndex].ToArray();
            var second = buffer.AsMemory().Slice(startIndex, data.Length).ToArray();
            var third = buffer.AsMemory()[(startIndex + data.Length)..].ToArray();
            Assert.True(first.All(_ => _ == 0));
            Assert.True(second.All(_ => _ == 255));
            Assert.True(third.All(_ => _ == 0));
            Assert.Equal(await file.GetLengthAsync(), first.Length + second.Length + third.Length);

            await file.CloseAsync();
            await file.DeleteAsync();
        }

        var fileInfo = new FileInfo("C:\\Users\\10087901428\\Downloads\\画像ダミーデータ 1\\画像ダミーデータ\\3.bmp");
        var dirInfo = new DirectoryInfo("C:\\Users\\10087901428\\Downloads\\画像ダミーデータ 1\\temp");

        // arrange
        using var cts = new CancellationTokenSource();

        var controller = await _fixture.Root.CreateControllerAsync(
            name: "AzureBlobStorageController",
            typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller.BlobStorageController, GeoLibrary.ORiN3.Provider.Azure.Storage",
            option: "{\"@Version\":\"" + version + "\",\"Account Name\":\"" + accountName + "\",\"Account Key\":\"" + accessKey + "\"}");

        // clean up
        {
            foreach (var blob in await list(controller))
            {
                await delete(controller, blob);
            }
        }

        // delete when no blobs
        {
            var result = await controller.ExecuteAsync("DeleteObject", makeArgs([Tuple.Create("Blob Path", (object?)"hoge")]));
            Assert.Equal(1, result["Result"]);
            Assert.Equal("BlobNotFound", result["Azure Error Code"]);
        }

        // upload from directory
        {
            var result = await controller.ExecuteAsync("UploadBlockFromDirectory", makeArgs([Tuple.Create("Directory Path", (object?)dirInfo.FullName)]));
            Assert.Equal(0, result["Result"]);
            foreach (var blob in await list(controller))
            {
                _output.WriteLine(blob);
            }

            var result2 = await controller.ExecuteAsync("UploadBlockFromDirectory", makeArgs([Tuple.Create("Directory Path", (object?)dirInfo.FullName), Tuple.Create("Overwrite", (object?)false)]));
            Assert.Equal(1, result2["Result"]);
            Assert.Equal(0, result2["Uploaded Count"]);
            Assert.Equal("BlobAlreadyExists", result2["Azure Error Code"]);
            foreach (var blob in await list(controller))
            {
                _output.WriteLine(blob);
            }

            var result3 = await controller.ExecuteAsync("UploadBlockFromDirectory", makeArgs([Tuple.Create("Directory Path", (object?)dirInfo.FullName), Tuple.Create("Overwrite", (object?)true), Tuple.Create("Prefix", (object?)"a")]));
            Assert.Equal(0, result3["Result"]);
            foreach (var blob in await list(controller))
            {
                _output.WriteLine(blob);
            }

            foreach (var blob in await list(controller))
            {
                await delete(controller, blob);
            }
        }

        // upload bytes
        {
            var blob = "a/b/hoge.txt";
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var result = await controller.ExecuteAsync("UploadBlock", makeArgs([Tuple.Create("Blob Path", (object?)blob), Tuple.Create("Bytes", (object?)bytes)]));
            Assert.Equal(0, result["Result"]);
            var blobs = await list(controller);
            Assert.Single(blobs);
            Assert.Equal(blob, blobs[0]);
            var file = await createBlock(controller, blob);
            var status = await file.GetStatusAsync();
            Assert.Equal((int)ORiN3ObjectStatus.Alive, status);
            await file.OpenAsync();
            var status2 = await file.GetStatusAsync();
            Assert.Equal((int)(ORiN3ObjectStatus.Alive | ORiN3ObjectStatus.Opened), status2);
            Assert.False(await file.CanWriteAsync());
            Assert.True(await file.CanReadAsync());
            var length = await file.GetLengthAsync();
            Assert.Equal(10, length);
            var buffer = new byte[length];
            await file.SeekAsync(5, ORiN3FileSeekOrigin.Current);
            var temp1 = await file.ReadAsync(buffer.AsMemory()[5..]);
            await file.SeekAsync(0, ORiN3FileSeekOrigin.Begin);
            var temp2 = await file.ReadAsync(buffer.AsMemory()[..5]);
            Assert.Equal(5, temp1);
            Assert.Equal(5, temp2);
            Assert.Equal(bytes, buffer);
            await file.SeekAsync(0, ORiN3FileSeekOrigin.Begin);
            try
            {
                await file.WriteAsync(new byte[] { 1, 2 });
            }
            catch (ProviderClientException e)
            {
                Assert.StartsWith("Blob is not writable.", e.Message);
            }
            await file.CloseAsync();
            var status3 = await file.GetStatusAsync();
            Assert.Equal((int)ORiN3ObjectStatus.Alive, status3);
            await file.DeleteAsync();

            // overwrite(true)
            var bytes2 = new byte[] { 1, 2, 3 };
            var result2 = await controller.ExecuteAsync("UploadBlock", makeArgs([Tuple.Create("Blob Path", (object?)blob), Tuple.Create("Bytes", (object?)bytes2), Tuple.Create("Overwrite", (object?)true)]));
            Assert.Equal(0, result2["Result"]);
            var blobs2 = await list(controller);
            Assert.Single(blobs2);
            Assert.Equal(blob, blobs2[0]);
            var file2 = await createBlock(controller, blob);
            await file2.OpenAsync();
            var properties = await file2.ExecuteAsync("GetProperties");
            var length2 = await file2.GetLengthAsync();
            var buffer2 = new byte[length2];
            await file2.ReadAsync(buffer2);
            Assert.Equal(bytes2, buffer2);
            await file2.CloseAsync();
            await file2.DeleteAsync();

            // overwrite(false) <= sometimes does not work
            //var bytes3 = new byte[] { 1, 2 };
            //var result3 = await controller.ExecuteAsync("UploadBlock", makeArgs([Tuple.Create("Blob Path", (object?)blob), Tuple.Create("Bytes", (object?)bytes3), Tuple.Create("Overwrite", (object?)false)]));
            //Assert.Equal(1, result3["Result"]);
            //Assert.Equal("BlobAlreadyExists", result2["Azure Error Code"]);
            //var blobs3 = await list(controller);
            //Assert.Single(blobs3);
            //Assert.Equal(blob, blobs3[0]);

            // ETag not match
            var result10 = await controller.ExecuteAsync("DeleteObject", makeArgs([Tuple.Create("Blob Path", (object?)blob), Tuple.Create("ETag", (object?)Guid.NewGuid().ToString())]));
            Assert.Equal(1, result10["Result"]);
            Assert.Equal("ConditionNotMet", result10["Azure Error Code"]);

            var result11 = await controller.ExecuteAsync("DeleteObject", makeArgs([Tuple.Create("Blob Path", (object?)blob), Tuple.Create("ETag", properties["ETag"])]));
            Assert.Equal(0, result11["Result"]);
        }

        // upload file
        {
            var result = await controller.ExecuteAsync("UploadBlockFromFile", makeArgs([Tuple.Create("File Path", (object?)fileInfo.FullName)]));
            Assert.Equal(0, result["Result"]);
            var blobs = await list(controller);
            Assert.Single(blobs);
            var file = await createBlock(controller, blobs[0]);
            await file.OpenAsync();
            var length = await file.GetLengthAsync();
            var buffer = new byte[length];
            await file.ReadAsync(buffer);
            var bytes = System.IO.File.ReadAllBytes(fileInfo.FullName);
            Assert.Equal(bytes, buffer);
            await file.CloseAsync();
            await file.DeleteAsync();
            await delete(controller, blobs[0]);
        }

        // upload file w/prefix
        {
            var result = await controller.ExecuteAsync("UploadBlockFromFile", makeArgs([Tuple.Create("File Path", (object?)fileInfo.FullName), Tuple.Create("Prefix", (object?)"a/b/c")]));
            Assert.Equal(0, result["Result"]);
            var blobs = await list(controller);
            Assert.Single(blobs);
            var file = await createBlock(controller, blobs[0]);
            await file.OpenAsync();
            var length = await file.GetLengthAsync();
            var buffer = new byte[length];
            await file.ReadAsync(buffer);
            var bytes = System.IO.File.ReadAllBytes(fileInfo.FullName);
            Assert.Equal(bytes, buffer);
            await file.CloseAsync();
            await file.DeleteAsync();
            await delete(controller, blobs[0]);
        }

        // append
        {
            var blob = "xyz/hoge.txt";
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            var result = await controller.ExecuteAsync("Append", makeArgs([Tuple.Create("Bytes", (object?)bytes), Tuple.Create("Blob Path", (object?)blob)]));
            Assert.Equal(0, result["Result"]);
            var eTag = result["ETag"];

            var file = await createAppend(controller, blob);
            await file.OpenAsync();
            var length = await file.GetLengthAsync();
            var buffer = new byte[length];
            await file.ReadAsync(buffer);
            Assert.Equal(bytes, buffer);
            await file.CloseAsync();
            await file.DeleteAsync();

            var result2 = await controller.ExecuteAsync("Append", makeArgs([Tuple.Create("Bytes", (object?)bytes), Tuple.Create("Blob Path", (object?)blob), Tuple.Create("ETag", (object?)Guid.NewGuid().ToString())]));
            Assert.Equal(1, result2["Result"]);
            Assert.Equal("ConditionNotMet", result2["Azure Error Code"]);

            var result3 = await controller.ExecuteAsync("Append", makeArgs([Tuple.Create("Bytes", (object?)bytes), Tuple.Create("Blob Path", (object?)blob), Tuple.Create("ETag", eTag)]));
            Assert.Equal(0, result3["Result"]);

            var file4 = await createAppend(controller, blob);
            await file4.OpenAsync();
            var length4 = await file4.GetLengthAsync();
            var buffer4 = new byte[length4];
            await file4.ReadAsync(buffer4);
            Assert.Equal(bytes.Concat(bytes), buffer4);
            await file4.CloseAsync();
            await file4.DeleteAsync();
        }

        // page blob
        {
            var blob = "ccc/fuga.txt";
            var bytes = new byte[] { 1, 2, 3, 4, 6, 7, 8 };

            var file = await controller.CreateFileAsync(
                name: "BlobFile",
                typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
                option: "{\"@Version\":\"" + version + "\",\"Container Name\":\"" + containerName + "\",\"Blob Path\":\"" + blob + "\"}");
            try
            {
                await file.OpenAsync();
            }
            catch (ProviderClientException e)
            {
                Assert.StartsWith("The specified blob does not exist.", e.Message);
            }
            await file.DeleteAsync();

            var file2 = await controller.CreateFileAsync(
                name: "BlobFile",
                typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
                option: "{\"@Version\":\"" + version + "\",\"Container Name\":\"" + containerName + "\",\"Blob Path\":\"" + blob + "\",\"Create New If Not Exists\":true}");
            try
            {
                await file2.OpenAsync();
            }
            catch (ProviderClientException e)
            {
                Assert.StartsWith("The \"Create New If Not Exists\" flag was set to true, but the Blob size (Length) was not specified.", e.Message);
            }
            await file2.DeleteAsync();

            var file3 = await controller.CreateFileAsync(
                name: "BlobFile",
                typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
                option: "{\"@Version\":\"" + version + "\",\"Container Name\":\"" + containerName + "\",\"Blob Path\":\"" + blob + "\",\"Create New If Not Exists\":true,\"Length\":100}");
            try
            {
                await file3.OpenAsync();
            }
            catch (ProviderClientException e)
            {
                Assert.StartsWith("An error occurred during Azure operation.", e.Message);
            }
            await file3.DeleteAsync();

            var testData = new List<Tuple<int, int>>()
            {
                Tuple.Create(0, 512),
                Tuple.Create(0, 1024),
                Tuple.Create(0, 1536),
                Tuple.Create(512, 512),
                Tuple.Create(1024, 512),
                Tuple.Create(0, 10),
                Tuple.Create(1, 10),
                Tuple.Create(510, 1),
                Tuple.Create(511, 1),
                Tuple.Create(512, 1),
                Tuple.Create(513, 1),
                Tuple.Create(1022, 1),
                Tuple.Create(1023, 1),
                Tuple.Create(1024, 1),
                Tuple.Create(0, 2),
                Tuple.Create(510, 2),
                Tuple.Create(511, 2),
                Tuple.Create(512, 2),
                Tuple.Create(1022, 2),
                Tuple.Create(1023, 2),
                Tuple.Create(1024, 2),
                Tuple.Create(0, 1024),
                Tuple.Create(0, 1025),
                Tuple.Create(511, 513),
                Tuple.Create(511, 514),
            };

            foreach (var it in testData)
            {
                await writeAndReadData(controller, it.Item1, it.Item2);
            }

            foreach (var it in await list(controller))
            {
                await delete(controller, it);
            }
        }

        // page blob etag
        {
            var blob = "hoge.txt";
            var file = await controller.CreateFileAsync(
                name: "BlobFile",
                typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
                option: "{\"@Version\":\"" + version + "\",\"Container Name\":\"" + containerName + "\",\"Blob Path\":\"" + blob + "\",\"Create New If Not Exists\":true,\"Length\":512}");
            var file2 = await controller.CreateFileAsync(
                name: "BlobFile",
                typeName: "GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.PageBlobFile, GeoLibrary.ORiN3.Provider.Azure.Storage",
                option: "{\"@Version\":\"" + version + "\",\"Container Name\":\"" + containerName + "\",\"Blob Path\":\"" + blob + "\"}");

            await file.OpenAsync();
            await file2.OpenAsync();
            var buffer = new byte[512];
            var buffer2 = new byte[1];
            buffer2[0] = 255;
            await file.ReadAsync(buffer);
            await file2.WriteAsync(buffer2);
            await file.SeekAsync(0, ORiN3FileSeekOrigin.Begin);
            var ex = await Assert.ThrowsAsync<ProviderClientException>(async () =>
            {
                _ = await file.ReadAsync(buffer);
            });
            Assert.Equal((int)AzureStorageProviderResultCode.AzureApiExecutionError, (int)ex.ResultCode);
            ex = await Assert.ThrowsAsync<ProviderClientException>(async () =>
            {
                await file.WriteAsync(buffer2);
            });
            Assert.Equal((int)AzureStorageProviderResultCode.AzureApiExecutionError, (int)ex.ResultCode);
            await file2.SeekAsync(0, ORiN3FileSeekOrigin.Begin);
            Assert.Equal(0, buffer[0]);
            await file2.ReadAsync(buffer);
            Assert.Equal(255, buffer[0]);

            ex = await Assert.ThrowsAsync<ProviderClientException>(async () =>
            {
                await file2.SetLengthAsync(1024);
            });
            Assert.Equal((int)GeoLibraryProviderResultCode.CanNotChangeLength, (int)ex.ResultCode);

            await file.CloseAsync();
            await file.OpenAsync();
            buffer[0] = 0;
            await file.ReadAsync(buffer);
            Assert.Equal(255, buffer[0]);

            await file2.CloseAsync();
            await file.DeleteAsync();
            await file2.DeleteAsync();
        }

        await controller.DeleteAsync();
    }
}
