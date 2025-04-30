using Colda.CommonUtilities;
using Design.ORiN3.Provider.Core.V1;
using GeoLibrary.ORiN3.Provider.TestBaseLib.Argument;
using ORiN3.Provider.Config;
using ORiN3.Provider.Core;
using ORiN3.Provider.Core.Abstract;
using System.IO.Pipes;
using System.Text.Json;

namespace GeoLibrary.ORiN3.Provider.TestBaseLib;

public class ProviderExecutor
{
    public static string Execute<T>(IProviderCoreStartupArgument argument, out Task providerTask, Action<string>? logger = null)
        where T : RootObjectBase, new()
    {
        var result = ORiN3ProviderConfigReader.ReadAsync(new FileInfo(argument.ConfigFile)).GetAwaiter().GetResult();
        var mutex = HandshakeOverProcess.Setup(argument.StartupHandshakeInformation.StartupFinishedEventName);
        logger?.Invoke($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Starting the provider. [Thread Safe Mode: {(argument.ThreadSafeMode ? "enabled" : "disabled")}, ProtocolType: {ToProtocolTypeString(argument.Endpoints[0].ProtocolType)}]");
        providerTask = Task.Factory.StartNew(delegate
        {
            new ProviderInitHelper<T>().Execute(argument);
        });
        HandshakeOverProcess.Wait(argument.StartupHandshakeInformation.StartupFinishedResponseEventName, mutex);
        logger?.Invoke($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Provider startup completed.");
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        jsonSerializerOptions.Converters.Add(new JsonConverterForInterface<IProviderInformation, ProviderInformation>());
        jsonSerializerOptions.Converters.Add(new JsonConverterForInterface<IProviderEndPoint, ProviderEndPoint>());
        var num = 10;
        for (var i = 0; i < num; i++)
        {
            try
            {
                logger?.Invoke($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Retrieving endpoint. (attempt {i + 1})");
                using var cancellationTokenSource = new CancellationTokenSource(1000);
                using var namedPipeClientStream = new NamedPipeClientStream(".", argument.PipeName, PipeDirection.In);
                namedPipeClientStream.ConnectAsync(cancellationTokenSource.Token).Wait();
                var array = new byte[1000];
                var num2 = namedPipeClientStream.Read(array, 0, array.Length);
                var uri = (JsonSerializer.Deserialize<IProviderInformation>(array.Take(num2).ToArray(), jsonSerializerOptions) ?? throw new InvalidDataException("Jsonフォーマットでのデータ取得失敗. [" + BitConverter.ToString(array, 0, num2) + "]")).EndPoints[0].Uri;
                logger?.Invoke($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Endpoint retrieval completed. => {uri}");
                return uri;
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception)
            {
                if (i == num - 1)
                {
                    throw;
                }
            }
        }

        throw new Exception();
    }

    public static string Execute<T>(int protocolType, out Task providerTask, Action<string>? logger = null)
        where T : RootObjectBase, new()
    {
        var endpointInfo = new ProviderCoreStartupArgumentEndpointInfo(protocolType, portNumber: 0);
        return Execute<T>(new ProviderCoreStartupArgument(endpointInfo), out providerTask, logger);
    }

    private static string ToProtocolTypeString(int protocolType)
    {
        return protocolType switch
        {
            0 => "HTTP",
            1 => "HTTPS",
            2 => "HTTP3",
            _ => protocolType.ToString()
        };
    }
}
