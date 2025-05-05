namespace GeoLibrary.ORiN3.Provider.BaseLib;

public enum GeoLibraryProviderResultCode
{
    //Common : 0x10100000-0x101000FF 
    Unknown = 0x10100000,
    //NotInitializedObject = 0x10100000,
    //NotConnected = 0x10100001,

    //Root : 0x10100100-0x101001FF
    FailedToCreateWorkingDir = 0x10100100,

    //Controller : 0x10100200-0x101002FF
    NotConnected = 0x10100200,
    InvalidArgument = 0x10100201,
    ArgumentNotFound = 0x10100202,
    //AlreadyConnected = 0x10100201,  //Serial Communication Only
    //CreateCommunicationClientFailed = 0x10100202,
    //ConnectFailed = 0x10100203,
    //GetStreamFailed = 0x10100204,
    //StartCommunicationFailed = 0x10100205,  //Serial Communication Only
    //CloseCommunicationClientFailed = 0x10100206,

    //File : 0x10100300-0x101003FF
    FileNotOpened = 0x10100300,
    FailedToWrite = 0x10100301,
    AlreadyOpened = 0x10100302,
    CanNotChangeLength = 0x10100303,

}

public static class GeoLibraryProviderResultCodeEx
{
    public static string ToErrorMessage(this GeoLibraryProviderResultCode resultCode, params object[] args)
    {
        return resultCode switch
        {
            GeoLibraryProviderResultCode.Unknown => $"Unknown error. {ArgToString([], args)}",
            GeoLibraryProviderResultCode.FailedToCreateWorkingDir => $"Failed to create a working directory. {ArgToString([ "path={0}" ], args)}",
            GeoLibraryProviderResultCode.CanNotChangeLength => $"Changing the data length is not supported. {ArgToString([], args)}",
            _ => $"An unexpected error has occurred. [code={resultCode}]",
        };
    }

    private static string ArgToString(string[] formats, params object[] args)
    {
        if (args.Length == 0)
        {
            return string.Empty;
        }

        if (formats.Length != args.Length)
        {
            return $"[{string.Join(", ", args.Select(_ => _ == null ? "{empty}" : _.ToString()))}]";
        }

        foreach (var i in Enumerable.Range(0, formats.Length))
        {
            formats[i] = ArgToString(formats[i], args, i);
        }
        return $"[{string.Join(", ", formats)}]";
    }

    private static string ArgToString(string format, object[] args, int index)
    {
        var target = string.Empty;
        if (args == null)
        {
            target = "{empty}";
        }
        else if (args.Length <= index)
        {
            target = "{empty}";
        }

        return string.Format(format, target);
    }
}
