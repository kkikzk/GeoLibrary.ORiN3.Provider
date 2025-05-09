using Amazon.S3;
using System.Net;
using static GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Controller.S3StorageController;

namespace GeoLibrary.ORiN3.Provider.AWS.S3;

internal class AmazonS3ConfigEx
{
    public interface IAmazonS3ConfigData
    {
        string RegionEndpoint { get; }
        bool UseHttps { get; }
        string ProxyUri { get; }
        string SrcIPAddress { get; }
    }

    public static AmazonS3Config GetConfig(IAmazonS3ConfigData configData)
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

        if (!string.IsNullOrEmpty(configData.SrcIPAddress))
        {
            config.HttpClientFactory = new CustomHttpClientFactory(configData.SrcIPAddress);
        }

        return config;
    }
}
