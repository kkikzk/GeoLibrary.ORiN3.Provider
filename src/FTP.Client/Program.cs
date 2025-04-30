using GeoLibrary.ORiN3.Provider.FTP.Client.O3Object.Root;
using ORiN3.Provider.Core.AspNetCore;

namespace GeoLibrary.ORiN3.Provider.FTP.Client;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var initHelper = new AspNetCoreInitHelper<FtpClientRootObject>();
        initHelper.Execute(args, builder);
    }
}
