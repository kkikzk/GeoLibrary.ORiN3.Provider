using GeoLibrary.ORiN3.Provider.TestBaseLib;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.Test.O3Object.Root;

public class RootObjectTest(ProviderTestFixture<RootObjectTest> fixture, ITestOutputHelper output) : TestBase<RootObjectTest>(fixture, output), IDisposable
{
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
