using Azure.Storage.Blobs.Specialized;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

internal class BlockBlobClientWrapper(BlockBlobClient client) : BlobBaseClientWrapper(client), IBlockBlobClient
{
}
