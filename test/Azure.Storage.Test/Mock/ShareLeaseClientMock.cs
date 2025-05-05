using Azure;
using Azure.Storage.Files.Shares.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;
using System.Reflection;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class ShareLeaseClientMock(string leaseId = null!) : IShareLeaseClient
{
    private readonly string _leaseId = leaseId;

    public Task<Response<ShareFileLease>> AcquireAsync(TimeSpan duration, CancellationToken token)
    {
        var ctor = typeof(ShareFileLease).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);

        var lease = (ShareFileLease)ctor!.Invoke(null);
        var leaseIdProp = typeof(ShareFileLease).GetProperty("LeaseId", BindingFlags.Instance | BindingFlags.Public);
        var leaseIdSetter = leaseIdProp!.GetSetMethod(true);
        leaseIdSetter!.Invoke(lease, [ _leaseId ]);

        return Task.FromResult(Response.FromValue(lease, null!));
    }
}