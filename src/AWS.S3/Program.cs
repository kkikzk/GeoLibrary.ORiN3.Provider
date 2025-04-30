using GeoLibrary.ORiN3.Provider.AWS.S3.O3Object.Root;
using ORiN3.Provider.Core.AspNetCore;

namespace GeoLibrary.ORiN3.Provider.AWS.S3;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var initHelper = new AspNetCoreInitHelper<S3RootObject>();
        initHelper.Execute(args, builder);
    }
}
