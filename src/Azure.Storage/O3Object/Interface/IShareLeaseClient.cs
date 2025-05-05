using Azure;
using Azure.Storage.Files.Shares.Models;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal interface IShareLeaseClient
{
    Task<Response<ShareFileLease>> AcquireAsync(TimeSpan duration, CancellationToken token);
}