using Azure;
using Azure.Storage.Files.Shares.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;
using GeoLibrary.ORiN3.Provider.BaseLib;
using ORiN3.Provider.Core;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Controller;

internal class FileStorageController : BaseStorageController
{
    protected override async Task<IDictionary<string, object?>> OnExecutingAsync(string commandName, IDictionary<string, object?> argument, CancellationToken token = default)
    {
        var result = commandName switch
        {
            "Upload" => await UploadAsync(argument, token).ConfigureAwait(false),
            "UploadFromFile" => await UploadFromFileAsync(argument, token).ConfigureAwait(false),
            "UploadFromDirectory" => await UploadFromDirectoryAsync(argument, token).ConfigureAwait(false),
            "ListFiles" => await ListFilesAsync(argument, token).ConfigureAwait(false),
            "ListDirectories" => await ListDirectoriesAsync(argument, token).ConfigureAwait(false),
            "DeleteFile" => await DeleteFileAsync(argument, token).ConfigureAwait(false),
            "DeleteDirectory" => await DeleteDirectoryAsync(argument, token).ConfigureAwait(false),
            //"GetLeaseId" => await GetLeaseIdAsync(argument, token).ConfigureAwait(false),
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

    private async Task<IDictionary<string, object?>> UploadAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(UploadAsync)} called.");
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.FilePath] = string.Empty,
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
            var shareName = ArgumentHelper.GetArgument<string>(argument, "Share Name");
            var filePath = ArgumentHelper.GetArgument<string>(argument, "Remote File Path");
            ORiN3ProviderLogger.LogTrace($"Bytes={data.Length}, Share Name={shareName}, Remote File Path={filePath}");

            var connectionString = ConnectionString.Create(this);
            var shareClient = new ShareClientEx(connectionString.ToString(), ProxyUri, shareName);
            await shareClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);
            var dirClient = shareClient.GetRootDirectoryClient();

            if (filePath.Contains('/'))
            {
                var split = filePath.Split('/');
                foreach (var dir in split[..^1])
                {
                    dirClient = dirClient.GetSubdirectoryClient(dir);
                    await dirClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);
                }
                filePath = split[^1];
            }

            var fileClient = dirClient.GetFileClient(filePath);
            await fileClient.CreateAsync(data.Length, new ShareFileRequestConditions(), token).ConfigureAwait(false);
            using var stream = new MemoryStream(data);
            await fileClient.UploadRangeAsync(new HttpRange(0, data.Length), stream, new ShareFileUploadRangeOptions(), token).ConfigureAwait(false);

            // Note: Unlike BlobClient.UploadAsync, ShareFileClient.UploadRangeAsync does not return a reliable ETag or LastModified.
            // Therefore, we must call GetPropertiesAsync to retrieve accurate metadata after upload.
            var properties = await fileClient.GetPropertiesAsync(new ShareFileRequestConditions(), token).ConfigureAwait(false);

            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.FilePath] = filePath;
            result[ParamNames.Uri] = fileClient.Uri.ToString();
            result[ParamNames.ETag] = properties.Value.ETag.ToString();
            result[ParamNames.LastModified] = properties.Value.LastModified.UtcDateTime;
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

    private async Task<IDictionary<string, object?>> UploadFromFileAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(UploadFromFileAsync)} called.");
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.FilePath] = string.Empty,
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
            var localPath = ArgumentHelper.GetArgument<string>(argument, "File Path");
            var shareName = ArgumentHelper.GetArgument<string>(argument, "Share Name");
            var remoteDirectoryPath = argument.TryGetValue("Remote Directory Path", out var p) ? (p as string ?? "").TrimEnd('/') : string.Empty;
            ORiN3ProviderLogger.LogTrace($"File Path={localPath}, Share Name={shareName}, Remote Directory Path={(string.IsNullOrEmpty(remoteDirectoryPath) ? "\"\"" : remoteDirectoryPath)}");

            var fileInfo = new FileInfo(localPath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException(null, fileInfo.FullName);
            }

            var remotePath = string.IsNullOrEmpty(remoteDirectoryPath) ? fileInfo.Name : $"{remoteDirectoryPath}/{fileInfo.Name}";

            var connectionString = ConnectionString.Create(this);
            var shareClient = new ShareClientEx(connectionString.ToString(), ProxyUri, shareName);
            await shareClient.CreateIfNotExistsAsync(token);
            var dirClient = shareClient.GetRootDirectoryClient();

            if (remotePath.Contains('/'))
            {
                var split = remotePath.Split('/');
                foreach (var dir in split[..^1])
                {
                    dirClient = dirClient.GetSubdirectoryClient(dir);
                    await dirClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);
                }
                remotePath = split[^1];
            }

            var fileClient = dirClient.GetFileClient(remotePath);
            await fileClient.CreateAsync(fileInfo.Length, new ShareFileRequestConditions(), token).ConfigureAwait(false);
            using var stream = System.IO.File.OpenRead(fileInfo.FullName);
            await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream, new ShareFileUploadRangeOptions(), token).ConfigureAwait(false);

            // Note: Unlike BlobClient.UploadAsync, ShareFileClient.UploadRangeAsync does not return a reliable ETag or LastModified.
            // Therefore, we must call GetPropertiesAsync to retrieve accurate metadata after upload.
            var props = await fileClient.GetPropertiesAsync(new ShareFileRequestConditions(), token).ConfigureAwait(false);

            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.FilePath] = string.IsNullOrEmpty(remoteDirectoryPath) ? fileInfo.Name : $"{remoteDirectoryPath}/{fileInfo.Name}";
            result[ParamNames.Uri] = fileClient.Uri.ToString();
            result[ParamNames.ETag] = props.Value.ETag.ToString();
            result[ParamNames.LastModified] = props.Value.LastModified.UtcDateTime;
        }
        catch (RequestFailedException ex)
        {
            result[ParamNames.Result] = ResponseCode.AzureError;
            result[ParamNames.AzureErrorCode] = ex.ErrorCode;
            result[ParamNames.HTTPStatus] = ex.Status;
            result[ParamNames.ErrorMessage] = ex.Message;
            result[ParamNames.StackTrace] = ex.StackTrace;
        }
        catch (Exception ex)
        {
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = ex.Message;
            result[ParamNames.StackTrace] = ex.StackTrace;
        }

        return result;
    }

    private async Task<IDictionary<string, object?>> UploadFromDirectoryAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(UploadFromDirectoryAsync)} called.");
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.UploadedCount] = 0,
            [ParamNames.AzureErrorCode] = string.Empty,
            [ParamNames.HTTPStatus] = 0,
            [ParamNames.ErrorMessage] = string.Empty,
            [ParamNames.StackTrace] = string.Empty
        };

        var uploaded = 0;

        try
        {
            var directoryPath = ArgumentHelper.GetArgument<string>(argument, "Directory Path");
            var shareName = ArgumentHelper.GetArgument<string>(argument, "Share Name");
            var remoteDirectoryPath = argument.TryGetValue("Remote Directory Path", out var p) ? (p as string ?? "").TrimEnd('/') : string.Empty;
            ORiN3ProviderLogger.LogTrace($"Directory Path={directoryPath}, Share Name={shareName}, Remote Directory Path={(string.IsNullOrEmpty(remoteDirectoryPath) ? "\"\"" : remoteDirectoryPath)}");

            var connectionString = ConnectionString.Create(this);
            var shareClient = new ShareClientEx(connectionString.ToString(), ProxyUri, shareName);
            await shareClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);
            var dirClient = shareClient.GetRootDirectoryClient();

            if (!string.IsNullOrEmpty(remoteDirectoryPath))
            {
                var split = remoteDirectoryPath.Split('/');
                foreach (var dir in split)
                {
                    dirClient = dirClient.GetSubdirectoryClient(dir);
                    await dirClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);
                }
            }

            foreach (var file in Directory.EnumerateFiles(directoryPath))
            {
                var fileInfo = new FileInfo(file);
                if (!fileInfo.Exists)
                {
                    continue;
                }

                var fileClient = dirClient.GetFileClient(fileInfo.Name);
                using var stream = System.IO.File.OpenRead(fileInfo.FullName);
                await fileClient.CreateAsync(stream.Length, new ShareFileRequestConditions(), token).ConfigureAwait(false);
                await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream, new ShareFileUploadRangeOptions(), token).ConfigureAwait(false);
                ++uploaded;
            }

            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.UploadedCount] = uploaded;
        }
        catch (RequestFailedException ex)
        {
            result[ParamNames.Result] = ResponseCode.AzureError;
            result[ParamNames.AzureErrorCode] = ex.ErrorCode;
            result[ParamNames.HTTPStatus] = ex.Status;
            result[ParamNames.ErrorMessage] = ex.Message;
            result[ParamNames.StackTrace] = ex.StackTrace;
            result[ParamNames.UploadedCount] = uploaded;
        }
        catch (Exception ex)
        {
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = ex.Message;
            result[ParamNames.StackTrace] = ex.StackTrace;
            result[ParamNames.UploadedCount] = uploaded;
        }

        return result;
    }

    private async Task<IDictionary<string, object?>> DeleteFileAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(DeleteFileAsync)} called.");
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
            var shareName = ArgumentHelper.GetArgument<string>(argument, "Share Name");
            var remoteFilePath = ArgumentHelper.GetArgument<string>(argument, "Remote File Path");
            ORiN3ProviderLogger.LogTrace($"Share Name={shareName}, Remote File Path={remoteFilePath}");

            var connectionString = ConnectionString.Create(this);
            var shareClient = new ShareClientEx(connectionString.ToString(), ProxyUri, shareName);
            var dirClient = shareClient.GetRootDirectoryClient();

            if (remoteFilePath.Contains('/'))
            {
                var split = remoteFilePath.Split('/');
                foreach (var dir in split[..^1])
                {
                    dirClient = dirClient.GetSubdirectoryClient(dir);
                }
                remoteFilePath = split[^1];
            }

            var fileClient = dirClient.GetFileClient(remoteFilePath);
            var response = await fileClient.DeleteAsync(token).ConfigureAwait(false);

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

    private async Task<IDictionary<string, object?>> DeleteDirectoryAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(DeleteDirectoryAsync)} called.");
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
            var shareName = ArgumentHelper.GetArgument<string>(argument, "Share Name");
            var remoteDirectoryPath = ArgumentHelper.GetArgument<string>(argument, "Remote Directory Path");
            ORiN3ProviderLogger.LogTrace($"Share Name={shareName}, Remote Directory Path={remoteDirectoryPath}");

            var connectionString = ConnectionString.Create(this);
            var shareClient = new ShareClientEx(connectionString.ToString(), ProxyUri, shareName);
            var dirClient = shareClient.GetRootDirectoryClient();

            if (remoteDirectoryPath.Contains('/'))
            {
                var split = remoteDirectoryPath.Split('/');
                foreach (var dir in split)
                {
                    dirClient = dirClient.GetSubdirectoryClient(dir);
                }
            }

            _ = await dirClient.DeleteIfExistsAsync(token).ConfigureAwait(false);
            result[ParamNames.Result] = ResponseCode.Success;
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

    private Task<IDictionary<string, object?>> ListFilesAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(ListFilesAsync)} called.");
        return ListItemsAsync(argument, isDirectory: false, token);
    }

    private Task<IDictionary<string, object?>> ListDirectoriesAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        ORiN3ProviderLogger.LogTrace($"{nameof(ListDirectoriesAsync)} called.");
        return ListItemsAsync(argument, isDirectory: true, token);

    }

    private async Task<IDictionary<string, object?>> ListItemsAsync(IDictionary<string, object?> argument, bool isDirectory, CancellationToken token)
    {
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.Count] = 0,
            [ParamNames.Names] = Array.Empty<string>(),
            [ParamNames.AzureErrorCode] = string.Empty,
            [ParamNames.HTTPStatus] = 0,
            [ParamNames.ErrorMessage] = string.Empty,
            [ParamNames.StackTrace] = string.Empty
        };

        try
        {
            var shareName = ArgumentHelper.GetArgument<string>(argument, "Share Name");
            string? directoryPath = null;
            if (argument.TryGetValue("Remote Directory Path", out var pathObj) && pathObj is string rawPath && !string.IsNullOrWhiteSpace(rawPath))
            {
                directoryPath = rawPath.TrimEnd('/');
            }
            ORiN3ProviderLogger.LogTrace($"Share Name={shareName}, Remote Directory Path={(string.IsNullOrEmpty(directoryPath) ? "\"\"" : directoryPath)}");

            var connectionString = ConnectionString.Create(this);
            var shareClient = new ShareClientEx(connectionString.ToString(), ProxyUri, shareName);
            var dirClient = shareClient.GetRootDirectoryClient();

            if (!string.IsNullOrEmpty(directoryPath))
            {
                var split = directoryPath.Split('/');
                foreach (var dir in split)
                {
                    dirClient = dirClient.GetSubdirectoryClient(dir);
                }
            }

            var list = new List<string>();
            await foreach (var item in dirClient.GetFilesAndDirectoriesAsync(token).ConfigureAwait(false))
            {
                if (isDirectory == item.IsDirectory)
                {
                    list.Add(item.Name);
                }
            }

            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.Count] = list.Count;
            result[ParamNames.Names] = list.ToArray();
            result[ParamNames.HTTPStatus] = 200;
        }
        catch (RequestFailedException ex)
        {
            result[ParamNames.Result] = ResponseCode.AzureError;
            result[ParamNames.AzureErrorCode] = ex.ErrorCode;
            result[ParamNames.HTTPStatus] = ex.Status;
            result[ParamNames.ErrorMessage] = ex.Message;
            result[ParamNames.StackTrace] = ex.StackTrace;
        }
        catch (Exception ex)
        {
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = ex.Message;
            result[ParamNames.StackTrace] = ex.StackTrace;
        }

        return result;
    }

    //private async Task<IDictionary<string, object?>> GetLeaseIdAsync(IDictionary<string, object?> argument, CancellationToken token)
    //{
    //    ORiN3ProviderLogger.LogTrace($"{nameof(GetLeaseIdAsync)} called.");
    //    var result = new Dictionary<string, object?>
    //    {
    //        [ParamNames.Result] = ResponseCode.Unknown,
    //        [ParamNames.LeasseId] = string.Empty,
    //        [ParamNames.AzureErrorCode] = string.Empty,
    //        [ParamNames.HTTPStatus] = 0,
    //        [ParamNames.ErrorMessage] = string.Empty,
    //        [ParamNames.StackTrace] = string.Empty
    //    };

    //    try
    //    {
    //        var shareName = ArgumentHelper.GetArgument<string>(argument, "Share Name");
    //        var duration = ArgumentHelper.GetArgument<int>(argument, "Duration");
    //        if (duration < 1 || 60 < duration)
    //        {
    //            throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.InvalidArgument, $"The duration must be specified between 1 and 60 seconds. [Duration={duration}]");
    //        }
    //        ORiN3ProviderLogger.LogTrace($"Share Name={shareName}");

    //        var connectionString = ConnectionString.Create(this);
    //        var shareClient = new ShareClientEx(connectionString.ToString(), shareName);
    //        await shareClient.CreateIfNotExistsAsync(token).ConfigureAwait(false);
    //        var leaseClient = shareClient.GetShareLeaseClient();
    //        var lease = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(duration), token).ConfigureAwait(false);
    //        result[ParamNames.Result] = ResponseCode.Success;
    //        result[ParamNames.LeasseId] = lease.Value.LeaseId;
    //    }
    //    catch (RequestFailedException requestFailedException)
    //    {
    //        result[ParamNames.Result] = ResponseCode.AzureError;
    //        result[ParamNames.AzureErrorCode] = requestFailedException.ErrorCode;
    //        result[ParamNames.HTTPStatus] = requestFailedException.Status;
    //        result[ParamNames.ErrorMessage] = requestFailedException.Message;
    //        result[ParamNames.StackTrace] = requestFailedException.StackTrace;
    //    }
    //    catch (Exception e)
    //    {
    //        result[ParamNames.Result] = ResponseCode.OtherError;
    //        result[ParamNames.ErrorMessage] = e.Message;
    //        result[ParamNames.StackTrace] = e.StackTrace;
    //    }

    //    return result;
    //}
}
