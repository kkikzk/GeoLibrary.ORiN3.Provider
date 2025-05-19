using Amazon;
using Amazon.S3;
using System.Net;

namespace GeoLibrary.ORiN3.Provider.AWS.S3;

internal class AmazonS3ConfigEx(AmazonS3Config config)
{
    public interface IAmazonS3ConfigData
    {
        string RegionEndpoint { get; }
        bool UseHttps { get; }
        string ProxyUri { get; }
    }

    private readonly AmazonS3Config _config = config;

    public RegionEndpoint RegionEndpoint { get { return _config.RegionEndpoint; } }

    public static AmazonS3ConfigEx GetConfig(IAmazonS3ConfigData configData)
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(configData.RegionEndpoint),
            UseHttp = !configData.UseHttps
        };

        if (!string.IsNullOrEmpty(configData.ProxyUri))
        {
            config.SetWebProxy(new WebProxy(configData.ProxyUri));
        }

        return new AmazonS3ConfigEx(config);
    }

    public static implicit operator AmazonS3Config(AmazonS3ConfigEx config)
    {
        return config._config;
    }
}
