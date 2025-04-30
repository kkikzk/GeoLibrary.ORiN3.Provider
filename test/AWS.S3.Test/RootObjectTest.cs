using GeoLibrary.ORiN3.Provider.AWS.S3.Test.Mock;
using GeoLibrary.ORiN3.Provider.TestBaseLib;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.Test;

public class RootObjectTest : IClassFixture<ProviderTestFixture<RootObjectTest>>, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ProviderTestFixture<RootObjectTest> _fixture;
    private readonly CancellationTokenSource _tokenSource = new(10000);

    public RootObjectTest(ProviderTestFixture<RootObjectTest> fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.InitAsync<S3RootObjectForTest>(_output, _tokenSource.Token).Wait();
    }

    public void Dispose()
    {
        try
        {
            _tokenSource.Dispose();
        }
        catch
        {
            // do nothing
        }
        GC.SuppressFinalize(this);
    }

    //[Fact]
    public async Task ListRegionEndpointTest()
    {
        var result = await _fixture.Root.ExecuteAsync("ListRegionEndpoint", _tokenSource.Token);
        Assert.Single(result);
        Assert.True(result.ContainsKey("System Name"));

        var resultValue = result["System Name"];
        Assert.NotNull(resultValue);
        Assert.Equal(typeof(string[]), resultValue.GetType());

        var endpoints = (string[])resultValue;
        Assert.True(0 < endpoints.Length);

        var index = 0;
        foreach (var it in (string[])resultValue)
        {
            _output.WriteLine($"[{index++}] {it}");
        }
    }
}
