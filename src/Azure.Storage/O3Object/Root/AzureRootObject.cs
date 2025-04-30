using Colda.CommonUtilities.IO;
using ORiN3.Provider.AWS.S3;
using ORiN3.Provider.Core;
using ORiN3.Provider.Core.Abstract;
using System.Reflection;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Root;

public class AzureRootObject : RootObjectBase
{
    static AzureRootObject()
    {
        LicenseCheckEnabled = false;
    }

    private static DirectoryInfo? _tempDir;
    internal static DirectoryInfo TempDir
    {
        get
        {
            if (_tempDir == null)
            {
                throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.Unknown, $"{nameof(_tempDir)} is null.");
            }
            return _tempDir;
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
            ORiN3ProviderLogger.LogTrace($"TempDir={tempDir.FullName}");
            if (!tempDir.Exists)
            {
                await tempDir.SafeCreateAsync().ConfigureAwait(false);
            }

            _tempDir = new DirectoryInfo(Path.Combine(tempDir.FullName, id.ToString()));
            if (!_tempDir.Exists)
            {
                await _tempDir.SafeCreateAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.Unknown, "", ex);
        }
    }

    protected override async Task OnShuttingDownAsync(CancellationToken token)
    {
        try
        {
            await TempDir.SafeDeleteAsync(recursive: true).ConfigureAwait(false);
        }
        catch
        {
            // do nothing.
        }

        await base.OnShuttingDownAsync(token).ConfigureAwait(false);
    }
}
