using Amazon.S3;
using Amazon.S3.Model;
using Design.ORiN3.Provider.V1;
using GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Interface;
using GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Root;
using GeoLibrary.ORiN3.Provider.BaseLib;
using ORiN3.Provider.Core;
using ORiN3.Provider.Core.Abstract;
using ORiN3.Provider.Core.OptionAnalyzer;
using ORiN3.Provider.Core.OptionAnalyzer.Attributes;
using System.Net;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller;

public class S3StorageController : ControllerBase, S3StorageController.IAccessData
{
    internal interface IAccessData : AmazonS3ConfigEx.IAmazonS3ConfigData
    {
        string AccessKey { get; }
        string SecretAccessKey { get; }
    }

    private class BaseStorageControllerOption
    {
        [JsonElementName("Region Endpoint")]
        public OptionValue<string> RegionEndpoint { get; set; } = new();

        [JsonElementName("Access Key")]
        public OptionValue<string> AccessKey { get; set; } = new();

        [Optional]
        [SecretValue]
        [JsonElementName("Secret Access Key")]
        public OptionValue<string> SecretAccessKey { get; set; } = new();

        [Optional]
        [JsonElementName("Use Https")]
        public OptionValue<bool> UseHttps { get; set; } = new();

        [Optional]
        [JsonElementName("Proxy Uri")]
        public OptionValue<string> ProxyUri { get; set; } = new();
    }

    public string RegionEndpoint { private set; get; } = string.Empty;
    public string AccessKey { private set; get; } = string.Empty;
    public string SecretAccessKey { private set; get; } = string.Empty;
    public bool UseHttps { private set; get; } = true;
    public string ProxyUri { private set; get; } = string.Empty;

    protected override async Task OnInitializingAsync(JsonElement option, bool needVersionCheck, object? fromParent, CancellationToken token)
    {
        await base.OnInitializingAsync(option, needVersionCheck, fromParent, token).ConfigureAwait(false);

        var opt = new OptionManager<BaseStorageControllerOption>(option).Analyze();
        RegionEndpoint = ArgumentHelper.GetArgument(opt.RegionEndpoint, nameof(opt.RegionEndpoint));
        AccessKey = ArgumentHelper.GetArgument(opt.AccessKey, nameof(opt.AccessKey));
        SecretAccessKey = ArgumentHelper.GetArgument(opt.SecretAccessKey, nameof(opt.SecretAccessKey));
        UseHttps = ArgumentHelper.GetArgumentOrDefault(opt.UseHttps, nameof(opt.UseHttps), true);
        ProxyUri = ArgumentHelper.GetArgumentOrDefault(opt.ProxyUri, nameof(opt.ProxyUri), string.Empty);
    }

    protected override Task<IFile> OnCreatingFileAsync(string name, string typeName, Type type, string option, object? _, CancellationToken token)
    {
        return base.OnCreatingFileAsync(name, typeName, type, option, new object[] { this, S3RootObject.WorkingDir }, token);
    }

    protected override async Task<IDictionary<string, object?>> OnExecutingAsync(string commandName, IDictionary<string, object?> argument, CancellationToken token = default)
    {
        var result = commandName switch
        {
            "UploadObject" => await UploadObjectAsync(argument, token).ConfigureAwait(false),
            "UploadObjectFromFile" => await UploadObjectFromFileAsync(argument, token).ConfigureAwait(false),
            "UploadObjectFromDirectory" => await UploadObjectFromDirectoryAsync(argument, token).ConfigureAwait(false),
            "ListObjects" => await ListObjectsAsync(argument, token).ConfigureAwait(false),
            "DeleteObject" => await DeleteObjectAsync(argument, token).ConfigureAwait(false),
            _ => await base.OnExecutingAsync(commandName, argument, token).ConfigureAwait(false),
        };

        try
        {
            ORiN3ProviderLogger.LogTrace($"Result={JsonSerializer.Serialize(result)}");
        }
        catch
        {
            // nothing to do.
        }

        return result;
    }

    private static async Task<IDictionary<string, object?>> ExecuteCoreAsync(IDictionary<string, object?> argument,
        Func<IDictionary<string, object?>, IDictionary<string, object?>, CancellationToken, Task> func, CancellationToken token)
    {
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.AWSErrorCode] = string.Empty,
            [ParamNames.HTTPStatus] = 0,
            [ParamNames.ErrorMessage] = string.Empty,
            [ParamNames.StackTrace] = string.Empty
        };

        try
        {
            await func(argument, result, token).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            ORiN3ProviderLogger.LogError($"AmazonS3Exception: {ex}");
            result[ParamNames.Result] = ResponseCode.AWSError;
            result[ParamNames.AWSErrorCode] = ex.ErrorCode;
            result[ParamNames.HTTPStatus] = (int)ex.StatusCode;
            result[ParamNames.ErrorMessage] = ex.Message;
            result[ParamNames.StackTrace] = ex.StackTrace;
        }
        catch (Exception ex)
        {
            ORiN3ProviderLogger.LogError($"Exception: {ex}");
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = ex.Message;
            result[ParamNames.StackTrace] = ex.StackTrace;
        }

        return result;
    }

    private static async Task<bool> ExistsObjectAsync(AmazonS3ClientEx client, string bucketName, string objectKey, CancellationToken token)
    {
        try
        {
            _ = await client.GetObjectMetadataAsync(bucketName, objectKey, token).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IDictionary<string, object?>> UploadObjectAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(UploadObjectAsync)} called.");
        return await ExecuteCoreAsync(argument, async (argument, result, token) =>
        {
            result[ParamNames.ObjectKey] = string.Empty;
            result[ParamNames.Uri] = string.Empty;
            result[ParamNames.ETag] = string.Empty;

            var data = ArgumentHelper.GetArgument<byte[]>(argument, "Bytes");
            var bucketName = ArgumentHelper.GetArgument<string>(argument, "Bucket Name");
            var objectKey = ArgumentHelper.GetArgument<string>(argument, "Object Key");
            var overwrite = ArgumentHelper.GetArgumentOrDefault(argument, "Overwrite", true);
            ORiN3ProviderLogger.LogTrace($"Uploading {bucketName}/{objectKey} ({data.Length} bytes), overwrite={overwrite}");

            var config = AmazonS3ConfigEx.GetConfig(this);
            using var client = new AmazonS3ClientEx(AccessKey, SecretAccessKey, config);
            if (!overwrite)
            {
                if (await ExistsObjectAsync(client, bucketName, objectKey, token).ConfigureAwait(false))
                {
                    ORiN3ProviderLogger.LogTrace($"Object '{objectKey}' found and overwrite is disabled.");
                    result[ParamNames.Result] = ResponseCode.OtherError;
                    result[ParamNames.ErrorMessage] = "Object exists and overwrite is disabled.";
                    return;
                }
            }

            using var stream = new MemoryStream(data);
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = stream
            };
            var response = await client.PutObjectAsync(request, token).ConfigureAwait(false);

            var regionName = config.RegionEndpoint.SystemName;
            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.ObjectKey] = objectKey;
            result[ParamNames.Uri] = $"https://{bucketName}.s3.{regionName}.amazonaws.com/{objectKey}";
            result[ParamNames.ETag] = response.ETag;
            result[ParamNames.HTTPStatus] = (int)response.HttpStatusCode;
        }, token).ConfigureAwait(false);
    }

    public async Task<IDictionary<string, object?>> UploadObjectFromFileAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(UploadObjectFromFileAsync)} called.");

        return await ExecuteCoreAsync(argument, async (argument, result, token) =>
        {
            result[ParamNames.ObjectKey] = string.Empty;
            result[ParamNames.Uri] = string.Empty;
            result[ParamNames.ETag] = string.Empty;

            var filePath = ArgumentHelper.GetArgument<string>(argument, "File Path");
            var bucketName = ArgumentHelper.GetArgument<string>(argument, "Bucket Name");
            var prefix = ArgumentHelper.GetArgumentOrDefault<string>(argument, "Prefix", null!);
            if (prefix != null && !prefix.EndsWith('/'))
            {
                prefix += "/";
            }
            var overwrite = ArgumentHelper.GetArgumentOrDefault(argument, "Overwrite", true);
            ORiN3ProviderLogger.LogTrace($"File Path={filePath}, Bucket Name={bucketName}, Prefix={prefix ?? "\"\""}, Overwrite={overwrite}");
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            var objectKey = (prefix ?? string.Empty) + fileInfo.Name;
            using var stream = System.IO.File.OpenRead(fileInfo.FullName);

            var config = AmazonS3ConfigEx.GetConfig(this);
            using var client = new AmazonS3ClientEx(AccessKey, SecretAccessKey, config);
            if (!overwrite)
            {
                if (await ExistsObjectAsync(client, bucketName, objectKey, token).ConfigureAwait(false))
                {
                    ORiN3ProviderLogger.LogTrace($"Object '{objectKey}' found and overwrite is disabled.");
                    result[ParamNames.Result] = ResponseCode.OtherError;
                    result[ParamNames.ErrorMessage] = "Object exists and overwrite is disabled.";
                    return;
                }
            }

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = stream
            };
            var response = await client.PutObjectAsync(request, token).ConfigureAwait(false);

            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.ObjectKey] = objectKey;
            var regionName = config.RegionEndpoint.SystemName;
            result[ParamNames.Uri] = $"https://{bucketName}.s3.{regionName}.amazonaws.com/{objectKey}";
            result[ParamNames.ETag] = response.ETag;
            result[ParamNames.HTTPStatus] = (int)response.HttpStatusCode;
        }, token).ConfigureAwait(false);
    }

    public async Task<IDictionary<string, object?>> UploadObjectFromDirectoryAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(UploadObjectFromDirectoryAsync)} called.");

        return await ExecuteCoreAsync(argument, async (argument, result, token) =>
        {
            result[ParamNames.UploadedCount] = 0;

            var directoryPath = ArgumentHelper.GetArgument<string>(argument, "Directory Path");
            var bucketName = ArgumentHelper.GetArgument<string>(argument, "Bucket Name");
            var prefix = ArgumentHelper.GetArgumentOrDefault<string?>(argument, "Prefix", null);
            if (prefix != null && !prefix.EndsWith('/'))
            {
                prefix += "/";
            }
            var overwrite = ArgumentHelper.GetArgumentOrDefault(argument, "Overwrite", true);
            ORiN3ProviderLogger.LogTrace($"Directory Path={directoryPath}, Bucket Name={bucketName}, Prefix={(prefix ?? "\"\"")}, Overwrite={overwrite}");
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            }

            var config = AmazonS3ConfigEx.GetConfig(this);
            using var client = new AmazonS3ClientEx(AccessKey, SecretAccessKey, config);

            HttpStatusCode lastStatus = 0;
            var counter = 0;
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                var fileName = Path.GetFileName(file);
                var objectKey = (prefix ?? string.Empty) + fileName;
                using var stream = System.IO.File.OpenRead(file);
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    InputStream = stream
                };
                var response = await client.PutObjectAsync(request, token).ConfigureAwait(false);
                lastStatus = response.HttpStatusCode;
                ++counter;
            }

            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.UploadedCount] = counter;
            result[ParamNames.HTTPStatus] = (int)lastStatus;
        }, token).ConfigureAwait(false);
    }

    public async Task<IDictionary<string, object?>> ListObjectsAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(ListObjectsAsync)} called.");
        return await ExecuteCoreAsync(argument, async (argument, result, token) =>
        {
            result[ParamNames.ObjectCount] = 0;
            result[ParamNames.ObjectNames] = Array.Empty<string>();

            var bucketName = ArgumentHelper.GetArgument<string>(argument, "Bucket Name");
            var prefix = ArgumentHelper.GetArgumentOrDefault<string?>(argument, "Prefix", null);
            ORiN3ProviderLogger.LogTrace($"Listing {bucketName}/ prefix='{prefix}'");

            var config = AmazonS3ConfigEx.GetConfig(this);
            using var client = new AmazonS3ClientEx(AccessKey, SecretAccessKey, config);
            var req = new ListObjectsV2Request { BucketName = bucketName, Prefix = prefix };
            var keys = new List<string>();

            do
            {
                var resp = await client.ListObjectsV2Async(req, token).ConfigureAwait(false);
                req.ContinuationToken = resp.NextContinuationToken;
                result[ParamNames.HTTPStatus] = (int)resp.HttpStatusCode;
                if (resp.KeyCount > 0)
                {
                    keys.AddRange(resp.S3Objects.Select(o => o.Key));
                }
            }
            while (!string.IsNullOrEmpty(req.ContinuationToken));

            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.ObjectCount] = keys.Count;
            result[ParamNames.ObjectNames] = keys.ToArray();
        }, token).ConfigureAwait(false);
    }

    public async Task<IDictionary<string, object?>> DeleteObjectAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(DeleteObjectAsync)} called.");
        return await ExecuteCoreAsync(argument, async (argument, result, token) =>
        {
            result[ParamNames.ObjectKey] = string.Empty;
            result[ParamNames.VersionId] = string.Empty;

            var bucketName = ArgumentHelper.GetArgument<string>(argument, "Bucket Name");
            var objectKey = ArgumentHelper.GetArgument<string>(argument, "Object Key");
            var versionId = ArgumentHelper.GetArgumentOrDefault<string?>(argument, "Version Id", null);
            ORiN3ProviderLogger.LogTrace($"Deleting {bucketName}/{objectKey}, VersionId='{versionId}'");

            var config = AmazonS3ConfigEx.GetConfig(this);
            using var client = new AmazonS3ClientEx(AccessKey, SecretAccessKey, config);
            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                VersionId = versionId
            };
            var response = await client.DeleteObjectAsync(request, token).ConfigureAwait(false);

            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.ObjectKey] = objectKey;
            result[ParamNames.VersionId] = response.VersionId;
            result[ParamNames.HTTPStatus] = (int)response.HttpStatusCode;
        }, token).ConfigureAwait(false);
    }
}
