namespace GeoLibrary.ORiN3.Provider.AWS.S3;

public enum ResponseCode
{
    Unknown = -1,       // Unknown error
    Success = 0,        // Operation completed successfully
    AWSError = 1,       // Error reported from AWS SDK or service
    OtherError = 2      // Other errors (network issues, unexpected 
}
