namespace GeoLibrary.ORiN3.Provider.Azure.Storage;

public enum ResponseCode
{
    Unknown = -1,       // Unknown error
    Success = 0,        // Operation completed successfully
    AzureError = 1,     // Error reported from Azure SDK or service
    OtherError = 2      // Other errors (network issues, unexpected
}
