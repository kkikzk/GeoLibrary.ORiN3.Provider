using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal class ShareClientEx(string connectionString, string shareName) : IShareClient
{
    private class MethodReverter(Func<string, string, IShareClient> createMethod) : IDisposable
    {
        private readonly Func<string, string, IShareClient> _createMethod = createMethod;

        public void Dispose()
        {
            CreateMethod = _createMethod;
        }
    }

    private readonly IShareClient _client = CreateMethod(connectionString, shareName);

    private static Func<string, string, IShareClient> CreateMethod { get; set; } = (connectionString, shareName) => new ShareClientWrapper(new ShareClient(connectionString, shareName));

    public static IDisposable SetCreateMethod(Func<string, string, IShareClient> createMethod)
    {
        var methodReverterex = new MethodReverter(CreateMethod);
        CreateMethod = createMethod;
        return methodReverterex;
    }

    public Task<Response<ShareInfo>> CreateIfNotExistsAsync(CancellationToken token)
    {
        return _client.CreateIfNotExistsAsync(token);
    }

    public IShareDirectoryClient GetRootDirectoryClient()
    {
        return _client.GetRootDirectoryClient();
    }

    public IShareLeaseClient GetShareLeaseClient(string leaseId = null!)
    {
        return _client.GetShareLeaseClient(leaseId);
    }
}
