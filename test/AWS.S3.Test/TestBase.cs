using Colda.Logging;
using GeoLibrary.ORiN3.Provider.AWS.S3.Test.Mock;
using GeoLibrary.ORiN3.Provider.TestBaseLib;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.Test;

public class TestBase<T> : IClassFixture<ProviderTestFixture<T>>, IDisposable
    where T : IClassFixture<ProviderTestFixture<T>>
{
    protected readonly ITestOutputHelper _output;
    protected readonly ProviderTestFixture<T> _fixture;
    protected readonly CancellationTokenSource _tokenSource = new(10000000);

    public TestBase(ProviderTestFixture<T> fixture, ITestOutputHelper output)
    {
        ColdaLogger.SetLogger(new TestOutputHelper(output));

        _fixture = fixture;
        _output = output;
        _fixture.InitAsync<S3RootObjectForTest>(_output, _tokenSource.Token).Wait();
    }

    public void Dispose()
    {
        try
        {
            ColdaLogger.SetLogger(NullLogger.Instance);
            _tokenSource.Dispose();
        }
        catch
        {
            // do nothing
        }
        GC.SuppressFinalize(this);
    }
}
