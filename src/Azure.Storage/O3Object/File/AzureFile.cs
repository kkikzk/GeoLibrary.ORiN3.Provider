using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Design.ORiN3.Provider.V1;
using ORiN3.Provider.Core.Abstract;
using ORiN3.Provider.Core.OptionAnalyzer;
using ORiN3.Provider.Core.OptionAnalyzer.Attributes;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File;

internal class AzureFile : FileBase, ConnectionString.IHasNameAndKey
{
    private AzureFileOption? _analyzedResult;
    private long _position;
    private ShareFileClient? _fileClient;
    private ShareFileProperties? _properties;

    private class AzureFileOption
    {
        [JsonElementName("Share Name")]
        public OptionValue<string> ShareName { get; set; } = new();
        [JsonElementName("Directory Name")]
        public OptionValue<string> DirectoryName { get; set; } = new();
        [JsonElementName("File Name")]
        public OptionValue<string> FileName { get; set; } = new();
    }

    public string AccountName { private set; get; } = string.Empty;
    public string AccessKey { private set; get; } = string.Empty;
    public bool UseHttps { private set; get; } = true;
    public string EndpointSuffix { private set; get; } = "core.windows.net";

    protected override Task OnInitializingAsync(JsonElement option, bool needVersionCheck, object? fromParent, CancellationToken token)
    {
        var temp = (Array)fromParent!;
        var nameAndKey = temp.GetValue(0) as ConnectionString.IHasNameAndKey;
        AccountName = nameAndKey!.AccountName;
        AccessKey = nameAndKey!.AccessKey;
        UseHttps = nameAndKey!.UseHttps;
        EndpointSuffix = nameAndKey!.EndpointSuffix;

        var optionManager = new OptionManager<AzureFileOption>(option);
        _analyzedResult = optionManager.Analyze();

        _position = 0;

        return base.OnInitializingAsync(option, needVersionCheck, fromParent, token);
    }

    protected async override Task OnOpeningAsync(JsonElement rootElement, IDictionary<string, object?> argument, CancellationToken token)
    {
        await base.OnOpeningAsync(rootElement, argument, token).ConfigureAwait(false);

        var connectionString = ConnectionString.Create(this);
        var shareClient = new ShareClient(connectionString.ToString(), _analyzedResult!.ShareName.Value);
        _ = await shareClient.CreateIfNotExistsAsync(cancellationToken: token).ConfigureAwait(false);

        var dirClient = shareClient.GetDirectoryClient(_analyzedResult!.DirectoryName.Value);
        _ = await dirClient.CreateIfNotExistsAsync(cancellationToken: token).ConfigureAwait(false);

        _fileClient = dirClient.GetFileClient(_analyzedResult!.FileName.Value);
        _properties = (await _fileClient.GetPropertiesAsync(cancellationToken: token).ConfigureAwait(false)).Value;
    }

    protected async override Task<long> OnGettingLengthAsync(CancellationToken token = default)
    {
        var properties = await _fileClient!.GetPropertiesAsync(cancellationToken: token).ConfigureAwait(false);
        return properties.Value.ContentLength;
    }

    protected override Task<bool> OnCanReadAsync(CancellationToken token = default)
    {
        return Task.FromResult(true);
    }

    protected override Task<bool> OnCanWriteAsync(CancellationToken token = default)
    {
        return base.OnCanWriteAsync(token);
    }

    protected async override Task<int> OnReadingAsync(Memory<byte> buffer, CancellationToken token = default)
    {
        var option = new ShareFileOpenReadOptions(false) { Position = _position, BufferSize = buffer.Length };
        using var stream = await _fileClient!.OpenReadAsync(option, token).ConfigureAwait(false);
        var result = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
        _position += result;
        return result;
    }

    protected async override Task<long> OnSeekingAsync(long offset, ORiN3FileSeekOrigin origin, CancellationToken token = default)
    {
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

    protected async override Task OnWritingAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
    {
        using var stream = await _fileClient!.OpenWriteAsync(true, _position, cancellationToken: token).ConfigureAwait(false);
         await stream.WriteAsync(buffer, token).ConfigureAwait(false);
        _position += buffer.Length;
    }
}
