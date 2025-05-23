﻿using Azure;
using Azure.Storage.Blobs.Models;
using Design.ORiN3.Common.V1.AutoGenerated;
using Design.ORiN3.Provider.V1;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.Base;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;
using GeoLibrary.ORiN3.Provider.BaseLib;
using ORiN3.Provider.Core;
using ORiN3.Provider.Core.OptionAnalyzer;
using ORiN3.Provider.Core.OptionAnalyzer.Attributes;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File;

internal class PageBlobFile : BlobFileBase<IPageBlobClient, PageBlobFile.PageBlobFileOption>
{
    private ETag? _eTag;
    private long _position;
    private IPageBlobClient? _blobClient;

    internal class PageBlobFileOption : BlobFileOption
    {
        [Optional]
        [JsonElementName("Create New If Not Exists")]
        public OptionValue<bool> CreateNewIfNotExists { get; set; } = new();

        [Optional]
        [JsonElementName("Length")]
        [NumericRange(0L, long.MaxValue)]
        public OptionValue<long> Length { get; set; } = new();
    }

    protected override IPageBlobClient GetClient(BlobContainerClientEx client, string blobPath)
    {
        return client.GetPageBlobClient(blobPath);
    }

    protected override Task OnInitializingAsync(JsonElement option, bool needVersionCheck, object? fromParent, CancellationToken token)
    {
        return base.OnInitializingAsync(option, needVersionCheck, fromParent, token);
    }

    protected override PageBlobFileOption AnalyzeOption(JsonElement option)
    {
        var optionManager = new OptionManager<PageBlobFileOption>(option);
        return optionManager.Analyze();
    }

    protected async override Task OnOpeningAsync(JsonElement rootElement, IDictionary<string, object?> argument, CancellationToken token)
    {
        // await base.OnOpeningAsync(rootElement, argument, token).ConfigureAwait(false); <= do not call.

        try
        {
            var connectionString = ConnectionString.Create(this);
            var containerName = ArgumentHelper.GetArgument(_analyzedResult!.ContainerName, nameof(_analyzedResult.ContainerName));
            var containerClient = new BlobContainerClientEx(connectionString.ToString(), ProxyUri, containerName);
            _blobClient = GetClient(containerClient, _analyzedResult!.BlobPath.Value);
            var exists = await _blobClient.ExistsAsync(token).ConfigureAwait(false);
            if (!exists.Value)
            {
                if (_analyzedResult.CreateNewIfNotExists.IsDefined && _analyzedResult.CreateNewIfNotExists.Value)
                {
                    if (!_analyzedResult.Length.IsDefined)
                    {
                        throw new GeoLibraryProviderException<AzureStorageProviderResultCode>(
                            AzureStorageProviderResultCode.InvalidCreationOption, $"The \"Create New If Not Exists\" flag was set to true, but the Blob size (Length) was not specified.");
                    }
                    var length = _analyzedResult.Length.Value;
                    await _blobClient.CreateAsync(length, token).ConfigureAwait(false);
                    ORiN3ProviderLogger.LogTrace($"Page Blob Created. [name={_analyzedResult!.BlobPath.Value}, length={length}]");
                }
                else
                {
                    throw new GeoLibraryProviderException<AzureStorageProviderResultCode>(
                        AzureStorageProviderResultCode.BlobNotFound, $"The specified blob does not exist. [path={_analyzedResult!.BlobPath.Value}]");
                }
            }

            //await _blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { CacheControl = "no-cache" }, cancellationToken: token);

            _position = 0L;
            _properties = (await _blobClient.GetPropertiesAsync(token).ConfigureAwait(false)).Value;
            _eTag = _properties.ETag;
        }
        catch (RequestFailedException ex)
        {
            var blobPath = (_analyzedResult != null) ? _analyzedResult!.BlobPath.Value : "unknown";
            var errorCode = string.IsNullOrEmpty(ex.ErrorCode) ? "empty" : ex.ErrorCode;
            var errorMessage = $"An error occurred during Azure operation. [Blob Path={blobPath}, Error Code={errorCode}, HTTP Status={ex.Status}, Message={ex.Message}]\r---\r{ex.StackTrace}]";
            throw new GeoLibraryProviderException<AzureStorageProviderResultCode>(AzureStorageProviderResultCode.AzureApiExecutionError, errorMessage, ex);
        }
    }

    protected override Task OnClosingAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        // return base.OnClosingAsync(argument, token); <= do not call

        _properties = null;
        _eTag = null;
        _position = 0;
        _blobClient = null;
        return Task.CompletedTask;
    }

    private static async Task<int> ReadAsync(Stream target, Memory<byte> buffer, CancellationToken token)
    {
        var counter = 0;
        for (var i = 0; i < 10; ++i)
        {
            counter += await target.ReadAsync(buffer[counter..], token).ConfigureAwait(false);
            if (counter == buffer.Length)
            {
                return counter;
            }
            await Task.Delay(10, token).ConfigureAwait(false);
        }

        throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.Unknown, $"Failed to read data from Azure Page Blob. [buffer.Length={buffer.Length}, read length={counter}]");
    }

    protected async override Task<int> OnReadingAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        if (_properties == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }

        try
        {
            var range = new HttpRange(_position, buffer.Length);
            var conditions = new BlobRequestConditions { IfMatch = _eTag };
            var response = await _blobClient!.DownloadAsync(range, conditions, token).ConfigureAwait(false);
            using var contentValue = response.Value;
            return await ReadAsync(contentValue.Content, buffer, token).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == 412) // <= ETag not match
        {
            var blobPath = (_analyzedResult != null) ? _analyzedResult!.BlobPath.Value : "unknown";
            var errorCode = string.IsNullOrEmpty(ex.ErrorCode) ? "empty" : ex.ErrorCode;
            var errorMessage = $"Failed to read data because the ETag changed. [Blob Path={blobPath}, Error Code={errorCode}, HTTP Status={ex.Status}, Message={ex.Message}]\r---\r{ex.StackTrace}]";
            throw new GeoLibraryProviderException<AzureStorageProviderResultCode>(AzureStorageProviderResultCode.AzureApiExecutionError, errorMessage, ex);
        }
        catch (RequestFailedException ex)
        {
            var blobPath = (_analyzedResult != null) ? _analyzedResult!.BlobPath.Value : "unknown";
            var errorCode = string.IsNullOrEmpty(ex.ErrorCode) ? "empty" : ex.ErrorCode;
            var errorMessage = $"An error occurred during Azure operation. [Blob Path={blobPath}, Error Code={errorCode}, HTTP Status={ex.Status}, Message={ex.Message}]\r---\r{ex.StackTrace}]";
            throw new GeoLibraryProviderException<AzureStorageProviderResultCode>(AzureStorageProviderResultCode.AzureApiExecutionError, errorMessage, ex);
        }
    }

    protected async override Task OnWritingAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
    {
        if (_properties == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }

        try
        {
            var calculator = new PageCalculator(_position, buffer.Length);
            var conditions = new BlobRequestConditions { IfMatch = _eTag };
            var content = await _blobClient!.DownloadAsync(new HttpRange(calculator.PageFirstIndex, calculator.Length), conditions, token).ConfigureAwait(false);
            var pageBuffer = new byte[calculator.Length];
            using (var contentValue = content.Value)
            {
                await ReadAsync(contentValue.Content, pageBuffer, token).ConfigureAwait(false);
            }
            buffer.CopyTo(pageBuffer.AsMemory((int)(_position - calculator.PageFirstIndex)));

            using var stream = new MemoryStream(pageBuffer);

            var uploadConditions = new PageBlobUploadPagesOptions { Conditions = new PageBlobRequestConditions() { IfMatch = _eTag } };
            var info = await _blobClient!.UploadPagesAsync(stream, calculator.PageFirstIndex, uploadConditions, token).ConfigureAwait(false);
            _eTag = info.Value.ETag;
        }
        catch (RequestFailedException ex) when (ex.Status == 412) // <= ETag not match
        {
            var blobPath = (_analyzedResult != null) ? _analyzedResult!.BlobPath.Value : "unknown";
            var errorCode = string.IsNullOrEmpty(ex.ErrorCode) ? "empty" : ex.ErrorCode;
            var errorMessage = $"Failed to write data because the ETag changed. [Blob Path={blobPath}, Error Code={errorCode}, HTTP Status={ex.Status}, Message={ex.Message}]\r---\r{ex.StackTrace}]";
            throw new GeoLibraryProviderException<AzureStorageProviderResultCode>(AzureStorageProviderResultCode.AzureApiExecutionError, errorMessage, ex);
        }
        catch (RequestFailedException ex)
        {
            var blobPath = (_analyzedResult != null) ? _analyzedResult!.BlobPath.Value : "unknown";
            var errorCode = string.IsNullOrEmpty(ex.ErrorCode) ? "empty" : ex.ErrorCode;
            var errorMessage = $"An error occurred during Azure operation. [Blob Path={blobPath}, Error Code={errorCode}, HTTP Status={ex.Status}, Message={ex.Message}]\r---\r{ex.StackTrace}]";
            throw new GeoLibraryProviderException<AzureStorageProviderResultCode>(AzureStorageProviderResultCode.AzureApiExecutionError, errorMessage, ex);
        }
    }

    protected override Task<bool> OnCanReadAsync(CancellationToken token = default)
    {
        if (_properties == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }
        return Task.FromResult(true);
    }

    protected override Task<bool> OnCanWriteAsync(CancellationToken token = default)
    {
        if (_properties == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }
        return Task.FromResult(true);
    }

    protected override Task<long> OnGettingLengthAsync(CancellationToken token = default)
    {
        if (_properties == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }
        return Task.FromResult(_properties!.ContentLength);
    }

    protected override Task OnSettingLengthAsync(long value, CancellationToken token = default)
    {
        if (_properties == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }
        throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.CanNotChangeLength);
    }

    protected async override Task<long> OnSeekingAsync(long offset, ORiN3FileSeekOrigin origin, CancellationToken token = default)
    {
        if (_properties == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }

        if (origin == ORiN3FileSeekOrigin.Begin)
        {
            _position = offset;
        }
        else if (origin == ORiN3FileSeekOrigin.Current)
        {
            _position += offset;
        }
        else if (origin == ORiN3FileSeekOrigin.End)
        {
            var length = await OnGettingLengthAsync(token).ConfigureAwait(false);
            _position = length + offset;
        }
        return _position;
    }

    protected override Task<IDictionary<string, object?>> GetPropertiesAsync(IDictionary<string, object?> _1, CancellationToken _2)
    {
        if (_properties == null)
        {
            throw new GeoLibraryProviderException(ResultCode.FileNotOpened);
        }

        ORiN3ProviderLogger.LogTrace($"{nameof(GetPropertiesAsync)} called.");
        var result = new Dictionary<string, object?>
        {
            [ParamNames.Result] = ResponseCode.Unknown,
            [ParamNames.BlobPath] = string.Empty,
            [ParamNames.ContentLength] = 0L,
            [ParamNames.ContentType] = string.Empty,
            [ParamNames.LastModified] = DateTime.MinValue,
            [ParamNames.CreatedOn] = DateTime.MinValue,
            [ParamNames.Archived] = false,
            [ParamNames.ServerEncrypted] = false,
            [ParamNames.ETag] = string.Empty,
            [ParamNames.CustomMetadata] = string.Empty,
            [ParamNames.ErrorMessage] = string.Empty,
            [ParamNames.StackTrace] = string.Empty,
        };

        try
        {
            result[ParamNames.Result] = ResponseCode.Success;
            result[ParamNames.BlobPath] = _analyzedResult!.BlobPath.Value;
            result[ParamNames.ContentLength] = _properties!.ContentLength;
            result[ParamNames.ContentType] = _properties!.ContentType;
            result[ParamNames.LastModified] = _properties!.LastModified.UtcDateTime;
            result[ParamNames.CreatedOn] = _properties!.CreatedOn.UtcDateTime;
#pragma warning disable IDE0075
            result[ParamNames.Archived] = _properties!.AccessTier == null ? false : _properties!.AccessTier == AccessTier.Archive;
#pragma warning restore IDE0075
            result[ParamNames.ServerEncrypted] = _properties!.IsServerEncrypted;
            result[ParamNames.ETag] = _properties!.ETag.ToString();
            result[ParamNames.CustomMetadata] = _properties!.Metadata == null ? null : JsonSerializer.Serialize(_properties!.Metadata);
            return Task.FromResult<IDictionary<string, object?>>(result);
        }
        catch (Exception e)
        {
            result[ParamNames.Result] = ResponseCode.OtherError;
            result[ParamNames.ErrorMessage] = e.Message;
            result[ParamNames.StackTrace] = e.StackTrace;
            return Task.FromResult<IDictionary<string, object?>>(result);
        }
    }
}
