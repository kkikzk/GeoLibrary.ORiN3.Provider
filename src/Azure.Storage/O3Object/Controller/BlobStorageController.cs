using Azure;
using Azure.Storage.Blobs.Models;
using Design.ORiN3.Provider.V1;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Root;
using ORiN3.Provider.Core;
using ORiN3.Provider.Core.Abstract;
using ORiN3.Provider.Core.OptionAnalyzer;
using ORiN3.Provider.Core.OptionAnalyzer.Attributes;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller;

internal class BlobStorageController : ControllerBase, ConnectionString.IHasNameAndKey
{
    private BlobStorageControllerOption? _analyzedResult;

    private class BlobStorageControllerOption
    {
        [JsonElementName("Account Name")]
        public OptionValue<string> AccountName { get; set; } = new();

        [SecretValue]
        [JsonElementName("Access Key")]
        public OptionValue<string> AccessKey { get; set; } = new();

        [Optional]
        [JsonElementName("Use Https")]
        public OptionValue<bool> UseHttps { get; set; } = new();

        [Optional]
        [JsonElementName("Endpoint Suffix")]
        public OptionValue<string> EndpointSuffix { get; set; } = new();
    }

    public string AccountName { private set; get; } = string.Empty;
    public string AccessKey { private set; get; } = string.Empty;
    public bool UseHttps { private set; get; } = true;
    public string EndpointSuffix { private set; get; } = "core.windows.net";

    protected override async Task OnInitializingAsync(JsonElement option, bool needVersionCheck, object? fromParent, CancellationToken token)
    {
        await base.OnInitializingAsync(option, needVersionCheck, fromParent, token);

        var optionManager = new OptionManager<BlobStorageControllerOption>(option);
        _analyzedResult = optionManager.Analyze();

        AccountName = _analyzedResult.AccountName.Value;
        AccessKey = _analyzedResult.AccessKey.Value;
#pragma warning disable IDE0075
        UseHttps = _analyzedResult.UseHttps.IsDefined ? _analyzedResult.UseHttps.Value : true;
#pragma warning restore IDE0075
        EndpointSuffix = _analyzedResult.EndpointSuffix.IsDefined ? _analyzedResult.EndpointSuffix.Value : "core.windows.net";
    }

    protected override Task<IFile> OnCreatingFileAsync(string name, string typeName, Type type, string option, object? _, CancellationToken token)
    {
        return base.OnCreatingFileAsync(name, typeName, type, option, new object[] { this, AzureRootObject.TempDir }, token);
    }

    protected override async Task<IDictionary<string, object?>> OnExecutingAsync(string commandName, IDictionary<string, object?> argument, CancellationToken token = default)
    {
        var result = commandName switch
        {
            "UploadBlock" => await UploadBlockAsync(argument, token).ConfigureAwait(false),
            "UploadBlockFromFile" => await UploadBlockFromFileAsync(argument, token).ConfigureAwait(false),
            "UploadBlockFromDirectory" => await UploadBlockFromDirectoryAsync(argument, token).ConfigureAwait(false),
            "ListObjects" => await ListObjectsAsync(argument, token).ConfigureAwait(false),
            "DeleteObject" => await DeleteObjectAsync(argument, token).ConfigureAwait(false),
            "Append" => await AppendAsync(argument, token).ConfigureAwait(false),
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

    private async Task<IDictionary<string, object?>> UploadBlockAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(AppendAsync)} called.");
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.BlobPath] = string.Empty,
            [ParamNames.Uri] = string.Empty,
            [ParamNames.ETag] = string.Empty,
            [ParamNames.LastModified] = DateTime.MinValue,
            [ParamNames.AzureErrorCode] = string.Empty,
            [ParamNames.HTTPStatus] = 0,
            [ParamNames.ErrorMessage] = string.Empty,
            [ParamNames.StackTrace] = string.Empty
        };

        try
        {
            var data = ArgumentHelper.GetArgument<byte[]>(argument, "Bytes");
            var containerName = ArgumentHelper.GetArgument<string>(argument, "Container Name");
            var blobPath = ArgumentHelper.GetArgument<string>(argument, "Blob Path");
            var overwrite = true;
            if (argument.ContainsKey("Overwrite"))
            {
                overwrite = ArgumentHelper.GetArgument<bool>(argument, "Overwrite");
            }
            ORiN3ProviderLogger.LogTrace($"Bytes={data}, Container Name={containerName}, Blob Path={blobPath}, Overwrite={overwrite}");

            var connectionString = ConnectionString.Create(this);
            var containerClient = new BlobContainerClientEx(connectionString.ToString(), containerName);
            await containerClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);
            var blobClient = containerClient.GetBlobClient(blobPath);

            using var stream = new MemoryStream(data);
            var response = await blobClient.UploadAsync(stream, overwrite, token).ConfigureAwait(false);
            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.BlobPath] = blobClient.Name;
            result[ParamNames.Uri] = blobClient.Uri.ToString();
            result[ParamNames.ETag] = response.Value.ETag.ToString();
            result[ParamNames.LastModified] = response.Value.LastModified.UtcDateTime;
        }
        catch (RequestFailedException requestFailedException)
        {
            result[ParamNames.Result] = ResponseCode.AzureError;
            result[ParamNames.AzureErrorCode] = requestFailedException.ErrorCode;
            result[ParamNames.HTTPStatus] = requestFailedException.Status;
            result[ParamNames.ErrorMessage] = requestFailedException.Message;
            result[ParamNames.StackTrace] = requestFailedException.StackTrace;
        }
        catch (Exception e)
        {
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = e.Message;
            result[ParamNames.StackTrace] = e.StackTrace;
        }

        return result;
    }

    private async Task<IDictionary<string, object?>> UploadBlockFromFileAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(UploadBlockFromFileAsync)} called.");
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.BlobPath] = string.Empty,
            [ParamNames.Uri] = string.Empty,
            [ParamNames.ETag] = string.Empty,
            [ParamNames.LastModified] = DateTime.MinValue,
            [ParamNames.AzureErrorCode] = string.Empty,
            [ParamNames.HTTPStatus] = 0,
            [ParamNames.ErrorMessage] = string.Empty,
            [ParamNames.StackTrace] = string.Empty
        };

        try
        {
            var filePath = ArgumentHelper.GetArgument<string>(argument, "File Path");
            var containerName = ArgumentHelper.GetArgument<string>(argument, "Container Name");
            string? prefix = null;
            if (argument.ContainsKey("Prefix"))
            {
                prefix = ArgumentHelper.GetArgument<string>(argument, "Prefix");
                if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith('/'))
                {
                    prefix += "/";
                }
            }
            var overwrite = true;
            if (argument.ContainsKey("Overwrite"))
            {
                overwrite = ArgumentHelper.GetArgument<bool>(argument, "Overwrite");
            }
            ORiN3ProviderLogger.LogTrace($"File Path={filePath}, Container Name={containerName}, Prefix={(string.IsNullOrEmpty(prefix) ? "\"\"" : prefix)}, Overwrite={overwrite}");

            var fileInfo = new FileInfo(filePath);
            var connectionString = ConnectionString.Create(this);
            var containerClient = new BlobContainerClientEx(connectionString.ToString(), containerName);
            await containerClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);
            var blobClient = containerClient.GetBlobClient(prefix == null ? fileInfo.Name : prefix + fileInfo.Name);
            using var uploadFileStream = System.IO.File.OpenRead(fileInfo.FullName);
            var response = await blobClient.UploadAsync(uploadFileStream, overwrite, token).ConfigureAwait(false);
            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.BlobPath] = blobClient.Name;
            result[ParamNames.Uri] = blobClient.Uri.ToString();
            result[ParamNames.ETag] = response.Value.ETag.ToString();
            result[ParamNames.LastModified] = response.Value.LastModified.UtcDateTime;
        }
        catch (RequestFailedException requestFailedException)
        {
            result[ParamNames.Result] = ResponseCode.AzureError;
            result[ParamNames.AzureErrorCode] = requestFailedException.ErrorCode;
            result[ParamNames.HTTPStatus] = requestFailedException.Status;
            result[ParamNames.ErrorMessage] = requestFailedException.Message;
            result[ParamNames.StackTrace] = requestFailedException.StackTrace;
        }
        catch (Exception e)
        {
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = e.Message;
            result[ParamNames.StackTrace] = e.StackTrace;
        }

        return result;
    }

    private async Task<IDictionary<string, object?>> UploadBlockFromDirectoryAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(UploadBlockFromDirectoryAsync)} called.");
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.UploadedCount] = 0,
            [ParamNames.AzureErrorCode] = string.Empty,
            [ParamNames.HTTPStatus] = 0,
            [ParamNames.ErrorMessage] = string.Empty,
            [ParamNames.StackTrace] = string.Empty
        };

        var counter = 0;
        try
        {
            var directoryPath = ArgumentHelper.GetArgument<string>(argument, "Directory Path");
            var containerName = ArgumentHelper.GetArgument<string>(argument, "Container Name");
            string? prefix = null;
            if (argument.ContainsKey("Prefix"))
            {
                prefix = ArgumentHelper.GetArgument<string>(argument, "Prefix");
                if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith('/'))
                {
                    prefix += "/";
                }
            }
            var overwrite = true;
            if (argument.ContainsKey("Overwrite"))
            {
                overwrite = ArgumentHelper.GetArgument<bool>(argument, "Overwrite");
            }
            ORiN3ProviderLogger.LogTrace($"Directory Path={directoryPath}, Container Name={containerName}, Prefix={(string.IsNullOrEmpty(prefix) ? "\"\"" : prefix)}, Overwrite={overwrite}");

            var directoryInfo = new DirectoryInfo(directoryPath);
            var connectionString = ConnectionString.Create(this);
            var containerClient = new BlobContainerClientEx(connectionString.ToString(), containerName);
            await containerClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);
            
            foreach (var file in Directory.EnumerateFiles(directoryInfo.FullName))
            {
                var name = new FileInfo(file).Name;
                var blobClient = containerClient.GetBlobClient(prefix == null ? name : prefix + name);
                using var uploadFileStream = System.IO.File.OpenRead(file);
                await blobClient.UploadAsync(uploadFileStream, overwrite, token).ConfigureAwait(false);
                ++counter;
            }
            result[ParamNames.Result] =  0;
            result[ParamNames.UploadedCount] = counter;
            
        }
        catch (RequestFailedException requestFailedException)
        {
            result[ParamNames.UploadedCount] = counter;
            result[ParamNames.Result] = ResponseCode.AzureError;
            result[ParamNames.AzureErrorCode] = requestFailedException.ErrorCode;
            result[ParamNames.HTTPStatus] = requestFailedException.Status;
            result[ParamNames.ErrorMessage] = requestFailedException.Message;
            result[ParamNames.StackTrace] = requestFailedException.StackTrace;
        }
        catch (Exception e)
        {
            result[ParamNames.UploadedCount] = counter;
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = e.Message;
            result[ParamNames.StackTrace] = e.StackTrace;
        }

        return result;
    }

    private async Task<IDictionary<string, object?>> DeleteObjectAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(DeleteObjectAsync)} called.");
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.AzureErrorCode] = string.Empty,
            [ParamNames.HTTPStatus] = 0,
            [ParamNames.ErrorMessage] = string.Empty,
            [ParamNames.StackTrace] = string.Empty
        };

        try
        {
            var containerName = ArgumentHelper.GetArgument<string>(argument, "Container Name");
            var blobName = ArgumentHelper.GetArgument<string>(argument, "Blob Path");
            ETag? eTag = null;
            if (argument.ContainsKey("ETag"))
            {
                eTag = new ETag(ArgumentHelper.GetArgument<string>(argument, "ETag"));
            }
            ORiN3ProviderLogger.LogTrace($"Container Name={containerName}, Blob Path={blobName}, ETag={eTag}");

            var connectionString = ConnectionString.Create(this);
            var containerClient = new BlobContainerClientEx(connectionString.ToString(), containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            var conditions = new BlobRequestConditions { IfMatch = eTag };
            using var response = await blobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, conditions, token).ConfigureAwait(false);
            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.HTTPStatus] = response.Status;
        }
        catch (RequestFailedException requestFailedException)
        {
            result[ParamNames.Result] = ResponseCode.AzureError;
            result[ParamNames.AzureErrorCode] = requestFailedException.ErrorCode;
            result[ParamNames.HTTPStatus] = requestFailedException.Status;
            result[ParamNames.ErrorMessage] = requestFailedException.Message;
            result[ParamNames.StackTrace] = requestFailedException.StackTrace;
        }
        catch (Exception e)
        {
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = e.Message;
            result[ParamNames.StackTrace] = e.StackTrace;
        }

        return result;
    }

    private async Task<IDictionary<string, object?>> ListObjectsAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(ListObjectsAsync)} called.");
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.ObjectCount] = 0,
            [ParamNames.ObjectNames] = Array.Empty<string>(),
            [ParamNames.AzureErrorCode] = string.Empty,
            [ParamNames.HTTPStatus] = 0,
            [ParamNames.ErrorMessage] = string.Empty,
            [ParamNames.StackTrace] = string.Empty
        };

        try
        {
            var containerName = ArgumentHelper.GetArgument<string>(argument, "Container Name");
            string? prefix = null;
            if (argument.ContainsKey("Prefix"))
            {
                prefix = ArgumentHelper.GetArgument<string>(argument, "Prefix");
                if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith('/'))
                {
                    prefix += "/";
                }
            }
            ORiN3ProviderLogger.LogTrace($"Container Name={containerName}, Prefix={prefix}");

            var connectionString = ConnectionString.Create(this);
            var containerClient = new BlobContainerClientEx(connectionString.ToString(), containerName);
            var segmentSize = 100;
            var resultSegment = containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: token).AsPages(default, segmentSize);
            var list = new List<string>();
            await foreach (var blobPage in resultSegment)
            {
                foreach (var blobItem in blobPage.Values)
                {
                    list.Add(blobItem.Name);
                }
            }
            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.ObjectCount] = list.Count;
            result[ParamNames.ObjectNames] = list.ToArray();
        }
        catch (RequestFailedException requestFailedException)
        {
            result[ParamNames.Result] = ResponseCode.AzureError;
            result[ParamNames.AzureErrorCode] = requestFailedException.ErrorCode;
            result[ParamNames.HTTPStatus] = requestFailedException.Status;
            result[ParamNames.ErrorMessage] = requestFailedException.Message;
            result[ParamNames.StackTrace] = requestFailedException.StackTrace;
        }
        catch (Exception e)
        {
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = e.Message;
            result[ParamNames.StackTrace] = e.StackTrace;
        }

        return result;
    }

    private async Task<IDictionary<string, object?>> AppendAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(AppendAsync)} called.");
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.BlobPath] = string.Empty,
            [ParamNames.Uri] = string.Empty,
            [ParamNames.ETag] = string.Empty,
            [ParamNames.LastModified] = DateTime.MinValue,
            [ParamNames.AzureErrorCode] = string.Empty,
            [ParamNames.HTTPStatus] = 0,
            [ParamNames.ErrorMessage] = string.Empty,
            [ParamNames.StackTrace] = string.Empty
        };

        try
        {
            var data = ArgumentHelper.GetArgument<byte[]>(argument, "Bytes");
            var containerName = ArgumentHelper.GetArgument<string>(argument, "Container Name");
            var blobPath = ArgumentHelper.GetArgument<string>(argument, "Blob Path");
            ETag? eTag = null;
            if (argument.ContainsKey("ETag"))
            {
                eTag = new ETag(ArgumentHelper.GetArgument<string>(argument, "ETag"));
            }
            ORiN3ProviderLogger.LogTrace($"Bytes={data}, Container Name={containerName}, Blob Path={blobPath}, ETag={eTag}");

            var connectionString = ConnectionString.Create(this);
            var containerClient = new BlobContainerClientEx(connectionString.ToString(), containerName);
            await containerClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);
            var blobClient = containerClient.GetAppendBlobClient(blobPath);
            await blobClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);

            using var stream = new MemoryStream(data);
            var options = new AppendBlobAppendBlockOptions() { Conditions = new() { IfMatch = eTag } };
            var response = await blobClient.AppendBlockAsync(stream, options, token).ConfigureAwait(false);
            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.BlobPath] = blobClient.Name;
            result[ParamNames.Uri] = blobClient.Uri.ToString();
            result[ParamNames.ETag] = response.Value.ETag.ToString();
            result[ParamNames.LastModified] = response.Value.LastModified.UtcDateTime;
        }
        catch (RequestFailedException requestFailedException)
        {
            result[ParamNames.Result] = ResponseCode.AzureError;
            result[ParamNames.AzureErrorCode] = requestFailedException.ErrorCode;
            result[ParamNames.HTTPStatus] = requestFailedException.Status;
            result[ParamNames.ErrorMessage] = requestFailedException.Message;
            result[ParamNames.StackTrace] = requestFailedException.StackTrace;
        }
        catch (Exception e)
        {
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = e.Message;
            result[ParamNames.StackTrace] = e.StackTrace;
        }

        return result;
    }
}
