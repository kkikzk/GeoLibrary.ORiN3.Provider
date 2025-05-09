using Colda.Logging;
using GeoLibrary.ORiN3.Provider.AWS.S3.Test.Mock;
using GeoLibrary.ORiN3.Provider.TestBaseLib;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.AWS.S3.Test.O3Object.Controller;

public class S3StorageControllerTest : IClassFixture<ProviderTestFixture<S3StorageControllerTest>>, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ProviderTestFixture<S3StorageControllerTest> _fixture;
    private readonly CancellationTokenSource _tokenSource = new(10000);

    public class TestOutputHelper(ITestOutputHelper logger) : ILogger
    {
        private readonly ITestOutputHelper _logger = logger;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var msg = formatter(state, exception);
            _logger.WriteLine($"[{logLevel}]: {msg}");
        }
    }

    public S3StorageControllerTest(ProviderTestFixture<S3StorageControllerTest> fixture, ITestOutputHelper output)
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
            ColdaLogger.SetLogger(null!);
            _tokenSource.Dispose();
        }
        catch
        {
            // do nothing
        }
        GC.SuppressFinalize(this);
    }


    [Fact]
    public async Task Test()
    {
        // arrange
        using var cts = new CancellationTokenSource(100000000);
        var controller = await _fixture.Root.CreateControllerAsync(
            name: "S3StorageController",
            typeName: "GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController, GeoLibrary.ORiN3.Provider.AWS.S3",
            option: "{\"@Version\":\"0.0.1\",\"Region Endpoint\":\"ap-northeast-1\",\"Access Key\":\"AKIAY5JCCRAUNLJFHGTZ\",\"Secret Access Key\":\"DrlVgRBq89KLKKjRk0nN03Vmvoh4uQ/GlpI4zmYHkfqKtiaKuExfH34zJByI0i3rscG6j9kYsBEfuYBwYth4rqLrOccqLf4Ca74k0bZtAGE=\"}",
            token: cts.Token);

        var arguments = new Dictionary<string, object?>()
        {
            { "Bucket Name", "mytestawsbacket2025" },
            { "Prefix", string.Empty },
        };

        var actDel = await controller.ExecuteAsync("DeleteObject", new Dictionary<string, object?>()
        {
            { "Bucket Name", "mytestawsbacket2025" },
            { "Object Key", "hogehoge2.txt" },
        }, cts.Token);

        var actual1 = await controller.ExecuteAsync("UploadObject", new Dictionary<string, object?>
        {
            { "Bucket Name", "mytestawsbacket2025" },
            { "Bytes", new byte[] { 1, 2, 3, 4, 5 } },
            { "Object Key", "hogehoge2.txt" },
            { "Overwrite", false },
        }, cts.Token);

        var actual = await controller.ExecuteAsync("ListObjects", arguments, cts.Token);
    }
}
