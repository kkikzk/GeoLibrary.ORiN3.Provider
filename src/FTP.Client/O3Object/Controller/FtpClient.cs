using Design.ORiN3.Provider.V1;
using Design.ORiN3.Provider.V1.Base;
using FluentFTP;
using ORiN3.Provider.Core;
using ORiN3.Provider.Core.Abstract;
using ORiN3.Provider.Core.OptionAnalyzer;
using ORiN3.Provider.Core.OptionAnalyzer.Attributes;
using System.Diagnostics;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.FTP.Client.O3Object.Controller;

internal class FtpClient : ControllerBase
{
    private enum FtpClientMode
    {
        Active,
        Passive,
    }

    private class FtpClientOption
    {
        [Optional]
        [JsonElementName("Host")]
        public OptionValue<string> Host { get; set; } = new();

        [Optional]
        [NumericRange(min: 1, max: 65535)]
        [JsonElementName("Port Number")]
        public OptionValue<int> PortNumber { get; set; } = new();

        [Optional]
        [JsonElementName("User")]
        public OptionValue<string> User { get; set; } = new();

        [Optional]
        [SecretValue]
        [JsonElementName("Password")]
        public OptionValue<string> Password { get; set; } = new();

        [Optional]
        [JsonElementName("Mode")]
        public OptionValue<FtpClientMode> Mode { get; set; } = new();
    }

    private string _host = "localhost";
    private int _portNumber = 21;
    private string _user = string.Empty;
    private string _password = string.Empty;
    private FtpClientMode _mode = FtpClientMode.Active;
    private AsyncFtpClient? _asyncFtpClient;
    internal AsyncFtpClient AsyncFtpClient
    {
        private get
        {
            return _asyncFtpClient ?? throw new FtpClientProviderException(FtpClientProviderResultCode.NotConnected, nameof(_asyncFtpClient));
        }
        set { _asyncFtpClient = value; }
    }

    protected override async Task OnInitializingAsync(JsonElement rootElement, bool needVersionCheck, object? fromParent, CancellationToken token)
    {
        await base.OnInitializingAsync(rootElement, needVersionCheck, fromParent, token);

        var optionManager = new OptionManager<FtpClientOption>(rootElement);
        var analyzedResult = optionManager.Analyze();

        if (analyzedResult.Host.IsDefined)
        {
            _host = analyzedResult.Host.Value;
        }

        if (analyzedResult.PortNumber.IsDefined)
        {
            _portNumber = analyzedResult.PortNumber.Value;
        }

        if (analyzedResult.User.IsDefined)
        {
            _user = analyzedResult.User.Value;
        }

        if (analyzedResult.Password.IsDefined)
        {
            _password = analyzedResult.Password.Value;
        }

        if (analyzedResult.Mode.IsDefined)
        {
            _mode = analyzedResult.Mode.Value;
        }
    }

    private class FtpLogger : IFtpLogger
    {
        public void Log(FtpLogEntry entry)
        {
            switch (entry.Severity)
            {
                case FtpTraceLevel.Info:
                    ORiN3ProviderLogger.LogInformation(entry.Message);
                    break;
                case FtpTraceLevel.Warn:
                    ORiN3ProviderLogger.LogWarning(entry.Message);
                    break;
                case FtpTraceLevel.Error:
                    ORiN3ProviderLogger.LogError(entry.Message);
                    break;
                case FtpTraceLevel.Verbose:
                default:
                    ORiN3ProviderLogger.LogTrace(entry.Message);
                    break;
            }

            if (entry.Exception != null)
            {
                ORiN3ProviderLogger.LogError(entry.Exception, entry.Message);
            }
        }
    }

    protected override Task<int> OnGettingStatusAsync(int currentStatus, CancellationToken token)
    {
        if (_asyncFtpClient != null && _asyncFtpClient.IsConnected)
        {
            currentStatus |= (int)ORiN3ObjectStatus.Connected;
        }
        return base.OnGettingStatusAsync(currentStatus, token);
    }

    protected override async Task OnOpeningAsync(JsonElement rootElement, IDictionary<string, object?> argument, CancellationToken token)
    {
        try
        {
            if (string.IsNullOrEmpty(_user))
            {
                AsyncFtpClient = new AsyncFtpClient(_host, _portNumber, logger: new FtpLogger());
            }
            else
            {
                var config = new FtpConfig();
                config.EncryptionMode = FtpEncryptionMode.None;
                config.DataConnectionType = FtpDataConnectionType.AutoActive;
                config.ValidateAnyCertificate = false;
                config.SslProtocols = System.Security.Authentication.SslProtocols.None;
                AsyncFtpClient = new AsyncFtpClient(_host, _user, _password, _portNumber, config, logger: new FtpLogger());
            }
            await AsyncFtpClient.AutoConnect(token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _asyncFtpClient = null;
            ORiN3ProviderLogger.LogError(e, "OnOpeningAsync failed.");
        }
    }

    protected override async Task OnClosingAsync(IDictionary<string, object?> argument, CancellationToken token)
    {
        var client = AsyncFtpClient;
        _asyncFtpClient = null;
        await client.Disconnect(token).ConfigureAwait(false);
        client.Dispose();
    }

    protected override Task<IFile> OnCreatingFileAsync(string name, string typeName, Type type, string option, object? fromParent, CancellationToken token)
    {
        return base.OnCreatingFileAsync(name, typeName, type, option, () => AsyncFtpClient, token);
    }

    protected override async Task<IDictionary<string, object?>> OnExecutingAsync(string commandName, IDictionary<string, object?> argument, CancellationToken token = default)
    {
        return commandName switch
        {
            "GetList" => await GetListAsync(GetArgument<string>("Path", argument), token).ConfigureAwait(false),
            "CreateFile" => await CreateFileAsync(GetArgument<string>("Path", argument), GetArgument<byte[]>("Content", argument), token).ConfigureAwait(false),
            "CreateDirectory" => await CreateDirectoryAsync(GetArgument<string>("Path", argument), token).ConfigureAwait(false),
            "MoveFile" => await MoveFileAsync(GetArgument<string>("Path", argument), GetArgument<string>("Dest", argument), token).ConfigureAwait(false),
            "DeleteFile" => await DeleteFileAsync(GetArgument<string>("Path", argument), token).ConfigureAwait(false),
            "DeleteDirectory" => await DeleteDirectoryAsync(GetArgument<string>("Path", argument), token).ConfigureAwait(false),
            _ => await base.OnExecutingAsync(commandName, argument, token),
        };
    }

    private static T? GetArgument<T>(string key, IDictionary<string, object?> argument)
    {
        if (!argument.ContainsKey(key))
        {
            throw new FtpClientProviderException(FtpClientProviderResultCode.ArgumentNotFound, $"Argument not found. [key={key}]");
        }

        var target = argument[key];
        try
        {
            return (T?)target;
        }
        catch
        {
            throw new FtpClientProviderException(FtpClientProviderResultCode.InvalidArgument, $"Invalid argument. [expected type={typeof(T).Name}, actual type={(target == null ? "null" : target.GetType().Name)}]");
        }
    }

    private static Dictionary<string, object?> ToErrorResult(Exception e)
    {
        return new Dictionary<string, object?>()
        {
            { "Result", FtpClientResponseCode.Failed },
            { "Error Message", e.Message },
            { "Detail", e.ToString() },
        };
    }

    private async Task<IDictionary<string, object?>> GetListAsync(string? path, CancellationToken token)
    {
        var files = new List<string>();
        var directories = new List<string>();
        var links = new List<string>();
        try
        {
            foreach (var it in await AsyncFtpClient.GetListing(path, token).ConfigureAwait(false))
            {
                if (it.Type == FtpObjectType.File)
                {
                    files.Add(it.Name);
                }
                else if (it.Type == FtpObjectType.Directory)
                {
                    directories.Add(it.Name);
                }
                else if (it.Type == FtpObjectType.Link)
                {
                    links.Add(it.Name);
                }
                else
                {
                    Debug.Assert(false);
                    throw new FtpClientProviderException(FtpClientProviderResultCode.Unknown, $"Invalid type. [type={it.Type}, name={it.Name}]");
                }
            }
        }
        catch (Exception e)
        {
            return ToErrorResult(e);
        }
        return new Dictionary<string, object?>()
        {
            { "Result", FtpClientResponseCode.Success },
            { "File", files.ToArray() },
            { "Directory", directories.ToArray() },
            { "Link", links.ToArray() },
        };
    }

    private async Task<IDictionary<string, object?>> CreateFileAsync(string? path, byte[]? content, CancellationToken token)
    {
        try
        {
            await AsyncFtpClient.UploadBytes(content, path, createRemoteDir: true, token: token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return ToErrorResult(e);
        }
        return new Dictionary<string, object?>() { { "Result", FtpClientResponseCode.Success } };
    }

    private async Task<IDictionary<string, object?>> CreateDirectoryAsync(string? path, CancellationToken token)
    {
        try
        {
            await AsyncFtpClient.CreateDirectory(path, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return ToErrorResult(e);
        }
        return new Dictionary<string, object?>() { { "Result", FtpClientResponseCode.Success } };
    }

    private async Task<IDictionary<string, object?>> MoveFileAsync(string? path, string? dest, CancellationToken token)
    {
        try
        {
            if (path == dest)
            {
                throw new FtpClientProviderException(FtpClientProviderResultCode.InvalidArgument, "The source file and the destination file are the same.");
            }

            await AsyncFtpClient.MoveFile(path, dest, token: token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return ToErrorResult(e);
        }
        return new Dictionary<string, object?>() { { "Result", FtpClientResponseCode.Success } };
    }

    private async Task<IDictionary<string, object?>> DeleteFileAsync(string? path, CancellationToken token)
    {
        try
        {
            await AsyncFtpClient.DeleteFile(path, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return ToErrorResult(e);
        }
        return new Dictionary<string, object?>() { { "Result", FtpClientResponseCode.Success } };
    }

    private async Task<IDictionary<string, object?>> DeleteDirectoryAsync(string? path, CancellationToken token)
    {
        try
        {
            await AsyncFtpClient.DeleteDirectory(path, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return ToErrorResult(e);
        }
        return new Dictionary<string, object?>() { { "Result", FtpClientResponseCode.Success } };
    }
}

public enum FtpClientResponseCode
{
    Success = 0,
    Failed = 1,
}
