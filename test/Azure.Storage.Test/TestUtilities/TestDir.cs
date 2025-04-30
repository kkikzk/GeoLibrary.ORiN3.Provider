using System.Reflection;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.TestUtilities;

public static class TestDir
{
    public static DirectoryInfo Get(string subDirName)
    {
        return new DirectoryInfo(Path.Combine(new FileInfo(Assembly.GetExecutingAssembly()!.Location).Directory!.FullName, "TestData", subDirName));
    }
}
