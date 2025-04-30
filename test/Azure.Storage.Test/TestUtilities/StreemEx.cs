namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.TestUtilities;

public static class StreemEx
{
    public static byte[] ToArray(this Stream stream)
    {
        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        return data;
    }

    public static byte[] ToArray(this FileInfo fileInfo)
    {
        using var stream = File.OpenRead(fileInfo.FullName);
        return stream.ToArray();
    }
}
