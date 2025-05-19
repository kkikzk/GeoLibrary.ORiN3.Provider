using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.TestBaseLib;

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
        try
        {
            var msg = formatter(state, exception);
            _logger?.WriteLine($"[{logLevel}]: {msg}");
        }
        catch
        {
            // nothing to do
        }
    }
}
