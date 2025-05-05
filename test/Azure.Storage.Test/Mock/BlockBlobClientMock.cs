using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class BlockBlobClientMock(string name, bool exists = true) : BlobBaseClientMock(name, exists), IBlockBlobClient
{
}
