using ORiN3.Provider.Core.OptionAnalyzer;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage;

public record ConnectionString(string Protocol, string AccountName, string AccountKey, string EndpointSuffix)
{
    public interface IHasNameAndKey
    {
        string AccountName { get; }
        string AccountKey { get; }
        bool UseHttps { get; }
        string EndpointSuffix { get; }
    }

    public override string ToString() =>
        $"DefaultEndpointsProtocol={Protocol};AccountName={AccountName};AccountKey={AccountKey};EndpointSuffix={EndpointSuffix}";

    public static ConnectionString Create(IHasNameAndKey nameAndKey)
    {
        var protocol = nameAndKey.UseHttps ? "https" : "http";
        return new ConnectionString(protocol, nameAndKey.AccountName, nameAndKey.AccountKey, nameAndKey.EndpointSuffix);
    }
}

public class ArgumentHelper
{
    public static T GetArgument<T>(OptionValue<T> option, string name)
    {
        if (option.IsDefined)
        {
            return option.Value;
        }
        throw new Exception($"Invalid argument. \"{name}\" does not exist.");
    }

    public static T GetArgument<T>(IDictionary<string, object?> argument, string key)
    {
        if (!argument.TryGetValue(key, out var value))
        {
            throw new Exception($"Invalid argument. \"{key}\" does not exist.");
        }
        else if (value is not T)
        {
            throw new Exception($"Invalid argument. \"{key}\" is not {typeof(T).Name}.");
        }
        return (T)value;
    }
}