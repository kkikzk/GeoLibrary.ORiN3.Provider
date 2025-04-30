namespace ORiN3.Provider.AWS.S3;

public enum GeoLibraryProviderResultCode
{
    //Common : 0x10100000-0x101000FF 
    Unknown = 0x10100000,
    //NotInitializedObject = 0x10100000,
    //NotConnected = 0x10100001,

    //Root : 0x10100100-0x101001FF
    FailedToCreateTempDir = 0x10100100,

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
    //GetValueFailed = 0x10100303,
    //SetValueFailed = 0x10100304,
}
