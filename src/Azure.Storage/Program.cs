using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Root;
using ORiN3.Provider.Core.AspNetCore;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var initHelper = new AspNetCoreInitHelper<AzureRootObject>();
        initHelper.Execute(args, builder);
    }
}
