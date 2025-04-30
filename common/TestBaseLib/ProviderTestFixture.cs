using Colda.CommonUtilities.Tasks;
using Design.ORiN3.Provider.V1;
using Grpc.Net.Client;
using Message.Client.ORiN3.Provider.V1;
using ORiN3.Provider.Core.Abstract;
using Xunit;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.TestBaseLib;

public class ProviderTestFixture<T> : IDisposable
    where T : IClassFixture<ProviderTestFixture<T>>
{
    private bool _initialized;
    private Task? _providerTask;
    private GrpcChannel? _channel;
    private string _providerUri = string.Empty;
    private IRootObject? _root;
    private readonly AsyncLock _asyncLock = new();

    public string ProviderUri
    {
        get
        {
            if (string.IsNullOrEmpty(_providerUri))
            {
                throw new InvalidOperationException("ProviderUri is not initialized.");
            }
            return _providerUri;
        }
    }

    public IRootObject Root
    {
        get
        {
            if (_root == null)
            {
                throw new InvalidOperationException("Root is not initialized.");
            }
            return _root;
        }
    }

    public async Task InitAsync<S>(ITestOutputHelper outputHelper, CancellationToken token)
        where S : RootObjectBase, new()
    {
        using (await _asyncLock.LockAsync(token).ConfigureAwait(false))
        {
            if (_initialized)
            {
                outputHelper.WriteLine("Initialized.");
                return;
            }

            var providerExecutorType = typeof(ProviderExecutor);
            var executeMethod = typeof(ProviderExecutor).GetMethod(nameof(ProviderExecutor.Execute), [
                typeof(int),
                typeof(Task).MakeByRefType(),
                typeof(Action<string>)
            ]) ?? throw new Exception("Execute method is not found.");

            var genericExecuteMethod = executeMethod.MakeGenericMethod(typeof(S));
            var protocolType = 0;
            var parameters = new object?[] { protocolType, null, (Action<string>)outputHelper.WriteLine };
            var result = genericExecuteMethod.Invoke(null, parameters);
            _providerUri = result as string ?? throw new InvalidOperationException("ProviderUri is null.");
            _providerTask = parameters[1] as Task; // out パラメータの値を取得

            _channel = GrpcChannel.ForAddress(ProviderUri);
            _root = await ORiN3RootObject.AttachAsync(_channel, 5000, token).ConfigureAwait(false);
            _initialized = true;
        }
    }

    public void Dispose()
    {
        Root.ShutdownAsync().Wait();
        Root.Dispose();
        _providerTask?.Wait();
        _channel?.Dispose();
        GC.SuppressFinalize(this);
    }
}
