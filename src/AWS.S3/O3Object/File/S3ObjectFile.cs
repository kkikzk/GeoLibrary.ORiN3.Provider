using GeoLibrary.ORiN3.Provider.BaseLib;
using ORiN3.Provider.Core.Abstract;
using ORiN3.Provider.Core.OptionAnalyzer;
using ORiN3.Provider.Core.OptionAnalyzer.Attributes;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.File;

public class S3ObjectFile : FileBase, AmazonS3ConfigEx.IAmazonS3ConfigData
{
    internal class BlobFileOption
    {
        [JsonElementName("Bucket Name")]
        public OptionValue<string> BucketName { get; set; } = new();

        [JsonElementName("Object Key")]
        public OptionValue<string> ObjectKey { get; set; } = new();
    }

    private DirectoryInfo? _workingDir;
    private readonly FileInfo? _tempFile;

    public string RegionEndpoint { private set; get; }
    public bool UseHttps { private set; get; }
    public string ProxyUri { private set; get; }
    public string SrcIPAddress { private set; get; }
    public string BucketName { private set; get; }
    public string ObjectKey { private set; get; }

    protected override Task OnInitializingAsync(JsonElement option, bool needVersionCheck, object? fromParent, CancellationToken token)
    {
        var temp = (Array)fromParent;
        var nameAndKey = temp.GetValue(0) as AmazonS3ConfigEx.IAmazonS3ConfigData;
        RegionEndpoint = nameAndKey.RegionEndpoint;
        UseHttps = nameAndKey.UseHttps;
        ProxyUri = nameAndKey.ProxyUri;
        SrcIPAddress = nameAndKey.SrcIPAddress;

        _workingDir = temp.GetValue(1) as DirectoryInfo;

        var optionManager = new OptionManager<BlobFileOption>(option);
        var result =  optionManager.Analyze();
        BucketName = ArgumentHelper.GetArgument(result.BucketName, "Bucket Name");
        ObjectKey = ArgumentHelper.GetArgument(result.BucketName, "Object Key");

        return base.OnInitializingAsync(option, needVersionCheck, fromParent, token);
    }

    //protected async override Task OnOpeningAsync(JsonElement rootElement, IDictionary<string, object?> argument, CancellationToken token)
    //{
    //    try
    //    {
    //        await base.OnOpeningAsync(rootElement, argument, token).ConfigureAwait(false);

    //        var tempFileName = Guid.NewGuid().ToString();
    //        _tempFile = new FileInfo(Path.Combine(_workingDir!.FullName, tempFileName));

    //        var connectionString = ConnectionString.Create(this);
    //        var containerName = ArgumentHelper.GetArgument(_analyzedResult!.ContainerName, nameof(_analyzedResult.ContainerName));
    //        var containerClient = new BlobContainerClientEx(connectionString.ToString(), ProxyUri, containerName);
    //        var blobClient = GetClient(containerClient, _analyzedResult!.BlobPath.Value);

    //        var exists = await blobClient.ExistsAsync(token).ConfigureAwait(false);
    //        if (!exists.Value)
    //        {
    //            throw new GeoLibraryProviderException<AzureStorageProviderResultCode>(
    //                AzureStorageProviderResultCode.BlobNotFound, $"The specified blob does not exist. [name={_analyzedResult!.BlobPath.Value}]");
    //        }

    //        //await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { CacheControl = "no-cache" }, cancellationToken: token);

    //        _properties = (await blobClient.GetPropertiesAsync(token).ConfigureAwait(false)).Value;
    //        _ = await blobClient.DownloadToAsync(_tempFile.FullName, token).ConfigureAwait(false);
    //        _stream = new FileStream(_tempFile.FullName, FileMode.Open, FileAccess.Read);
    //    }
    //    catch (RequestFailedException ex)
    //    {
    //        var blobPath = (_analyzedResult != null) ? _analyzedResult!.BlobPath.Value : "unknown";
    //        var errorCode = string.IsNullOrEmpty(ex.ErrorCode) ? "empty" : ex.ErrorCode;
    //        var errorMessage = $"An error occurred during Azure operation. [Blob Path={blobPath}, Error Code={errorCode}, HTTP Status={ex.Status}, Message={ex.Message}]\r---\r{ex.StackTrace}]";
    //        throw new GeoLibraryProviderException<AzureStorageProviderResultCode>(AzureStorageProviderResultCode.AzureApiExecutionError, errorMessage, ex);
    //    }
    //}
}
