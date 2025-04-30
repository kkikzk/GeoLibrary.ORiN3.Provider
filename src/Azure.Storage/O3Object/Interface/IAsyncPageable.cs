using Azure;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

internal interface IAsyncPageable<T> : IAsyncEnumerable<T> where T : notnull
{
    IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = default, int? pageSizeHint = default);
}
