﻿using Amazon.S3;
using Colda.CommonUtilities.IO;
using Design.ORiN3.Common.V1.AutoGenerated;
using Design.ORiN3.Provider.V1;
using GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller;
using GeoLibrary.ORiN3.Provider.BaseLib;
using ORiN3.Provider.Core.Abstract;
using ORiN3.Provider.Core.OptionAnalyzer;
using ORiN3.Provider.Core.OptionAnalyzer.Attributes;
using ORiN3.Provider.Core.ProviderException;
using System.IO;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.File;

public class S3ObjectFile : FileBase, S3StorageController.IAccessData
{
    internal class BlobFileOption
    {
        [JsonElementName("Bucket Name")]
        public OptionValue<string> BucketName { get; set; } = new();

        [JsonElementName("Object Key")]
        public OptionValue<string> ObjectKey { get; set; } = new();
    }

    private DirectoryInfo? _workingDir;
    private FileInfo? _tempFile;
    private FileStream? _stream;

    public string RegionEndpoint { private set; get; } = string.Empty;
    public string AccessKey { private set; get; } = string.Empty;
    public string SecretAccessKey { private set; get; } = string.Empty;
    public bool UseHttps { private set; get; } = true;
    public string ProxyUri { private set; get; } = string.Empty;
    public string BucketName { private set; get; } = string.Empty;
    public string ObjectKey { private set; get; } = string.Empty;

    protected override Task OnInitializingAsync(JsonElement option, bool needVersionCheck, object? fromParent, CancellationToken token)
    {
        var temp = (Array)fromParent!;
        var nameAndKey = temp.GetValue(0) as S3StorageController.IAccessData;
        RegionEndpoint = nameAndKey!.RegionEndpoint;
        AccessKey = nameAndKey.AccessKey;
        SecretAccessKey = nameAndKey.SecretAccessKey;
        UseHttps = nameAndKey.UseHttps;
        ProxyUri = nameAndKey.ProxyUri;

        _workingDir = temp.GetValue(1) as DirectoryInfo;

        var optionManager = new OptionManager<BlobFileOption>(option);
        var result =  optionManager.Analyze();
        BucketName = ArgumentHelper.GetArgument(result.BucketName, "Bucket Name");
        ObjectKey = ArgumentHelper.GetArgument(result.ObjectKey, "Object Key");

        return base.OnInitializingAsync(option, needVersionCheck, fromParent, token);
    }

    protected async override Task OnOpeningAsync(JsonElement rootElement, IDictionary<string, object?> argument, CancellationToken token)
    {
        try
        {
            await base.OnOpeningAsync(rootElement, argument, token).ConfigureAwait(false);

            var tempFileName = Guid.NewGuid().ToString();
            _tempFile = _workingDir!.CombineWithFileName(tempFileName);

            var config = AmazonS3ConfigEx.GetConfig(this);
            using var client = new AmazonS3Client(AccessKey, SecretAccessKey, config);
            //public virtual Task<GetObjectResponse> GetObjectAsync(string bucketName, string key, System.Threading.CancellationToken cancellationToken = default(CancellationToken))
            using var result = await client.GetObjectAsync(BucketName, ObjectKey, token).ConfigureAwait(false);
            using (var stream = new FileStream(_tempFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None, (int)result.ContentLength))
            {
                await result.ResponseStream.CopyToAsync(stream, token).ConfigureAwait(false);
            }
            _stream = new FileStream(_tempFile.FullName, FileMode.Open, FileAccess.Read);
        }
        catch (AmazonS3Exception ex)
        {
            var errorCode = string.IsNullOrEmpty(ex.ErrorCode) ? "empty" : ex.ErrorCode;
            var errorMessage = $"An error occurred during AWS operation. [Bucket Name={BucketName}, Object Key={ObjectKey}, Error Code={errorCode}, Message={ex.Message}]\r---\r{ex.StackTrace}]";
            throw new GeoLibraryProviderException<AWSS3ProviderResultCode>(AWSS3ProviderResultCode.AWSApiExecutionError, errorMessage, ex);
        }
    }

    protected override Task<long> OnGettingLengthAsync(CancellationToken token = default)
    {
        if (_stream == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }
        return Task.FromResult(_stream!.Length);
    }

    protected override Task<long> OnSeekingAsync(long offset, ORiN3FileSeekOrigin origin, CancellationToken _ = default)
    {
        if (_stream == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }
        return Task.FromResult(_stream!.Seek(offset, (SeekOrigin)origin));
    }

    protected override Task<bool> OnCanReadAsync(CancellationToken token = default)
    {
        if (_stream == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }
        return Task.FromResult(true);
    }

    protected override Task<bool> OnCanWriteAsync(CancellationToken token = default)
    {
        if (_stream == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }
        return Task.FromResult(false);
    }

    protected async override Task<int> OnReadingAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        if (_stream == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }
        return await _stream.ReadAsync(buffer, token).ConfigureAwait(false);
    }

    protected override Task OnWritingAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
    {
        if (_stream == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }
        throw new ProviderCoreException($"Object is not writable. [Type={GetType().FullName}]", (ResultCode)AWSS3ProviderResultCode.ObjectNotWritable);
    }

    protected override async Task OnClosingAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        if (_stream != null)
        {
            try
            {
                _stream.Close();
                _stream.Dispose();
                _stream = null;
            }
            catch
            {
                // nothing to do.
            }
        }

        if (_tempFile != null && _tempFile.Exists)
        {
            try
            {
                await _tempFile.SafeDeleteAsync().ConfigureAwait(false);
            }
            catch
            {
                // nothing to do.
            }
            _tempFile.Delete();
            _tempFile = null;
        }

        await base.OnClosingAsync(argument, token).ConfigureAwait(false);
    }
}
