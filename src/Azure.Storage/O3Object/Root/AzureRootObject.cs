using Colda.CommonUtilities.IO;
using GeoLibrary.ORiN3.Provider.BaseLib;
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

    private static DirectoryInfo? _workingDir;
    internal static DirectoryInfo WorkingDir
    {
        get
        {
            if (_workingDir == null)
            {
                throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.Unknown, $"[info={nameof(AzureRootObject)}.{nameof(_workingDir)} is null.]");
            }
            return _workingDir;
        }
    }

    private static DirectoryInfo GetWorkingRootDirectory()
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
            var workingRootDir = GetWorkingRootDirectory();
            ORiN3ProviderLogger.LogTrace($"Working root directory={workingRootDir.FullName}");
            if (!workingRootDir.Exists)
            {
                await workingRootDir.SafeCreateAsync().ConfigureAwait(false);
            }

            _workingDir = new DirectoryInfo(Path.Combine(workingRootDir.FullName, id.ToString()));
            if (!_workingDir.Exists)
            {
                await _workingDir.SafeCreateAsync().ConfigureAwait(false);
                ORiN3ProviderLogger.LogTrace($"Working directory created. [path={_workingDir.FullName}]");
            }
        }
        catch (GeoLibraryProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.FailedToCreateWorkingDir, $"Error occurred. [message={ex}]");
        }
    }

    protected override async Task OnShuttingDownAsync(CancellationToken token)
    {
        try
        {
            await WorkingDir.SafeDeleteAsync(recursive: true).ConfigureAwait(false);
            ORiN3ProviderLogger.LogTrace($"Working Directory deleted. [path={WorkingDir.FullName}]");
        }
        catch
        {
            // do nothing.
        }

        await base.OnShuttingDownAsync(token).ConfigureAwait(false);
    }
}
