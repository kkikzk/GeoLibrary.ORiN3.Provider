using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File.Base;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File;

internal class AppendBlobFile : BlobFileBase<IBlobBaseClient, BlobFileOption>
{
    protected override IBlobBaseClient GetClient(BlobContainerClientEx client, string blobPath)
    {
        return client.GetAppendBlobClient(blobPath);
    }
}