using Azure;
using Azure.Core.Pipeline;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;
using System.Net;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal class ShareClientEx(string connectionString, string proxyUri, string shareName) : IShareClient
{
    private class MethodReverter(Func<string,string, string, IShareClient> createMethod) : IDisposable
    {
        private readonly Func<string, string, string, IShareClient> _createMethod = createMethod;

        public void Dispose()
        {
            CreateMethod = _createMethod;
        }
    }

    private readonly IShareClient _client = CreateMethod(connectionString, proxyUri, shareName);

    private static Func<string, string, string, IShareClient> CreateMethod { get; set; } = (connectionString, proxyUri, shareName) =>
    {
        if (string.IsNullOrEmpty(proxyUri))
        {
            return new ShareClientWrapper(new ShareClient(connectionString, shareName));
        }
        else
        {
            var proxy = new WebProxy(proxyUri);
            var handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true
            };
            var options = new ShareClientOptions
            {
                Transport = new HttpClientTransport(handler)
            };
            return new ShareClientWrapper(new ShareClient(connectionString, shareName, options));
        }
    };

    public static IDisposable SetCreateMethod(Func<string, string, string, IShareClient> createMethod)
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
