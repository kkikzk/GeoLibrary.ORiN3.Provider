using Amazon;
using Colda.CommonUtilities.IO;
using GeoLibrary.ORiN3.Provider.BaseLib;
using ORiN3.Provider.Core;
using ORiN3.Provider.Core.Abstract;
using System.Reflection;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Root;

internal class S3RootObject : RootObjectBase
{
    static S3RootObject()
    {
        LicenseCheckEnabled = false;
    }

    private static DirectoryInfo? _workingDir;
    internal static DirectoryInfo WorkingDir
    {
        get
        {
            if (_workingDir == null)
            {
                throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.Unknown, $"{nameof(_workingDir)} is null.");
            }
            return _workingDir;
        }
    }

    private static DirectoryInfo GetCurrentDirectory()
    {
        try
        {
            var executableFile = Assembly.GetExecutingAssembly().Location;
            var locationDir = new FileInfo(executableFile).Directory;
            if (locationDir == null)
            {
                ORiN3ProviderLogger.LogError($"Failed to get executable file location. [executableFile={executableFile}]");
                throw new Exception();
            }
            return new DirectoryInfo(Path.Combine(locationDir.FullName, "temp"));
        }
        catch
        {
            return new DirectoryInfo(Directory.GetCurrentDirectory());
        }
    }

    protected override async Task OnInitializingAsync(JsonElement option, bool needVersionCheck, object? fromParent, CancellationToken token)
    {
        await base.OnInitializingAsync(option, needVersionCheck, fromParent, token).ConfigureAwait(false);

        try
        {
            var id = Guid.NewGuid();
            var tempDir = GetCurrentDirectory();
            ORiN3ProviderLogger.LogTrace($"WorkingDir={tempDir.FullName}");
            if (!tempDir.Exists)
            {
                await tempDir.SafeCreateAsync().ConfigureAwait(false);
            }

            _workingDir = new DirectoryInfo(Path.Combine(tempDir.FullName, id.ToString()));
            if (!_workingDir.Exists)
            {
                await _workingDir.SafeCreateAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.Unknown, "", ex);
        }
    }

    protected override Task<IDictionary<string, object?>> OnExecutingAsync(string commandName, IDictionary<string, object?> argument, CancellationToken token = default)
    {
        if (commandName == "ListRegionEndpoint")
        {
            return Task.FromResult<IDictionary<string, object?>>(new Dictionary<string, object?>()
            {
                { "System Name", RegionEndpoint.EnumerableAllRegions.Select(_ => _.SystemName).ToArray() },
            });
        }
        return base.OnExecutingAsync(commandName, argument, token);
    }

    protected override async Task OnShuttingDownAsync(CancellationToken token)
    {
        try
        {
            await WorkingDir.SafeDeleteAsync(recursive: true).ConfigureAwait(false);
        }
        catch
        {
            // do nothing.
        }

        await base.OnShuttingDownAsync(token).ConfigureAwait(false);
    }
}
