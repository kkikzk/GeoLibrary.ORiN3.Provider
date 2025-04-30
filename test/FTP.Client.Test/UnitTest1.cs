using Colda.CommonUtilities;
using GeoLibrary.ORiN3.Provider.FTP.Client.Test.Mock;
using GeoLibrary.ORiN3.Provider.TestBaseLib;
using System.Net;
using System.Net.Sockets;
using Xunit.Abstractions;

namespace GeoLibrary.ORiN3.Provider.FTP.Client.Test;

public class SimpleFtpServer(ITestOutputHelper output, string rootDirectory, int port = 21)
{
    private readonly ITestOutputHelper _output = output;
    private readonly string _rootDirectory = rootDirectory;
    private readonly int _port = port;

    public async Task StartAsync(TcpListener listener)
    {
        try
        {
            _output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Started the FTP Server. [Port: {((IPEndPoint)listener.LocalEndpoint).Port}]");
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"FTPサーバーが例外で停止しました。{ex}");
        }
    }

    private static string ToArgString(string[] parts)
    {
        if (parts.Length == 1)
        {
            return "No Args";
        }
        return $"Args: {string.Join(", ", parts.Skip(1))}";
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream) { AutoFlush = true };

        await writer.WriteLineAsync("220 Welcome to Simple FTP Server");

        string? command;
        while ((command = await reader.ReadLineAsync()) != null)
        {
            var parts = command.Split(' ');
            var cmd = parts[0].ToUpper();

            switch (cmd)
            {
                case "AUTH":
                    _output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] AUTH Command. [Client: {client.Client.RemoteEndPoint}, {ToArgString(parts)}]");
                    await writer.WriteLineAsync("534 TLS not supported");
                    break;
                case "USER":
                    _output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] USER Command. [Client: {client.Client.RemoteEndPoint}, {ToArgString(parts)}]");
                    await writer.WriteLineAsync("331 User name okay, need password");
                    break;
                case "PASS":
                    _output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] PASS Command. [Client: {client.Client.RemoteEndPoint}, {ToArgString(parts)}]");
                    await writer.WriteLineAsync("230 User logged in");
                    break;
                case "PWD":
                    _output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] PWD Command. [Client: {client.Client.RemoteEndPoint}, {ToArgString(parts)}]");
                    await writer.WriteLineAsync($"257 \"{_rootDirectory}\" is current directory");
                    break;
                case "QUIT":
                    _output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] QUIT Command. [Client: {client.Client.RemoteEndPoint}, {ToArgString(parts)}]");
                    await writer.WriteLineAsync("221 Goodbye");
                    return;
                case "TYPE":
                    _output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] TYPE Command. [Client: {client.Client.RemoteEndPoint}, {ToArgString(parts)}]");
                    await writer.WriteLineAsync("200 Type set to I");
                    return;
                case "FEAT":
                    _output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] FEAT Command. [Client: {client.Client.RemoteEndPoint}, {ToArgString(parts)}]");
                    await writer.WriteLineAsync("211 no-features");
                    return;
                default:
                    _output.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {cmd} Command. [Client: {client.Client.RemoteEndPoint}, {ToArgString(parts)}]");
                    await writer.WriteLineAsync("502 Command not implemented");
                    break;
            }
        }
    }
}

public class UnitTest1 : IClassFixture<ProviderTestFixture<UnitTest1>>, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ProviderTestFixture<UnitTest1> _fixture;
    private readonly CancellationTokenSource _tokenSource = new(10000);
    private const string ControllerTypeName = "GeoLibrary.ORiN3.Provider.FTP.Client.O3Object.Controller.FtpClient, GeoLibrary.ORiN3.Provider.FTP.Client";

    public UnitTest1(ProviderTestFixture<UnitTest1> fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.InitAsync<FtpClientRootObjectForTest>(_output, _tokenSource.Token).Wait();
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
    public async Task Test1()
    {
        var listener = new TcpListener(IPEndPoint.Parse($"{IPAddress.Loopback}:0"));
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task.Run(() =>
        {
            var server = new SimpleFtpServer(_output, @"C:\FtpRoot");
            server.StartAsync(listener);
        });

        var controllerOption = new Dictionary<string, object> {
            { "@Version", "1.0.0" },
            { "Host", IPAddress.Loopback.ToString() },
            { "Port Number", port },
            { "User", "hoge" },
        };
        var option = JsonHelper.Serialize(controllerOption);

        var result = await _fixture.Root.CreateControllerAsync($"{nameof(UnitTest1)}.{nameof(Test1)}", ControllerTypeName, option, _tokenSource.Token);
        await result.ConnectAsync(_tokenSource.Token);
        await result.ExecuteAsync("GetList", new Dictionary<string, object?>() { { "Path", "C:\\" } }, _tokenSource.Token);
        await result.ExecuteAsync("CreateFile", new Dictionary<string, object?>() { { "Path", "C:\\" }, { "Content", new byte[] { 1, 2 } } }, _tokenSource.Token);
        await result.CloseAsync(_tokenSource.Token);
    }
}