using GeoLibrary.ORiN3.Provider.BaseLib;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.File;

internal struct PageCalculator
{
    public long PageFirstIndex { private set; get; }
    public long PageLastIndex { private set; get; }
    public long Length { private set; get; }

    public PageCalculator(long dataPosition, int dataLength, long pageSize = 512L)
    {
        if (dataPosition < 0)
        {
            throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.Unknown, $"{nameof(PageCalculator)}.{nameof(dataPosition)} must not be negative.");
        }
        if (dataLength <= 0)
        {
            throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.Unknown, $"{nameof(PageCalculator)}.{nameof(dataLength)} must be a positive, non-zero value.");
        }
        if (pageSize <= 0)
        {
            throw new GeoLibraryProviderException(GeoLibraryProviderResultCode.Unknown, $"{nameof(PageCalculator)}.{nameof(pageSize)} must be a positive, non-zero value.");
        }

        PageFirstIndex = dataPosition / pageSize * pageSize;
        PageLastIndex = ((dataPosition + dataLength + pageSize - 1) / pageSize * pageSize) - 1;
        Length = PageLastIndex - PageFirstIndex + 1;
    }
}
