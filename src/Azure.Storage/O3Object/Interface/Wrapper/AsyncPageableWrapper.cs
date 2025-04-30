using Azure;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface.Wrapper;

internal class AsyncPageableWrapper<T>(AsyncPageable<T> inner) : IAsyncPageable<T> where T : notnull
{
    private readonly AsyncPageable<T> _inner = inner;

    public IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = default, int? pageSizeHint = default)
        => _inner.AsPages(continuationToken, pageSizeHint);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => _inner.GetAsyncEnumerator(cancellationToken);
}
