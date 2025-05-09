namespace GeoLibrary.ORiN3.Provider.Azure.Storage;

public record ConnectionString(string Protocol, string AccountName, string AccountKey, string EndpointSuffix)
{
    public interface IHasNameAndKey
    {
        string AccountName { get; }
        string AccountKey { get; }
        bool UseHttps { get; }
        string EndpointSuffix { get; }
        string ProxyUri { get; }
    }

    public override string ToString() =>
        $"DefaultEndpointsProtocol={Protocol};AccountName={AccountName};AccountKey={AccountKey};EndpointSuffix={EndpointSuffix}";

    public static ConnectionString Create(IHasNameAndKey nameAndKey)
    {
        var protocol = nameAndKey.UseHttps ? "https" : "http";
        return new ConnectionString(protocol, nameAndKey.AccountName, nameAndKey.AccountKey, nameAndKey.EndpointSuffix);
    }
}
