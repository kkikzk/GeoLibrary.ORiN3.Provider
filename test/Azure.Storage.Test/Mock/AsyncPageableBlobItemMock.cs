using Azure;
using Azure.Storage.Blobs.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class AsyncPageableBlobItemMock : IAsyncPageable<BlobItem>
{
    private readonly List<BlobItem> _items;

    public AsyncPageableBlobItemMock(int itemCount)
    {
        var counter = 0;
        _items = new int[itemCount].Select(_ => BlobsModelFactory.BlobItem(
            name: $"{++counter}.txt",
            deleted: false,
            snapshot: null,
            versionId: null,
            isLatestVersion: true,
            properties: BlobsModelFactory.BlobItemProperties(accessTierInferred: true)
        )).ToList();
    }

    public AsyncPageableBlobItemMock()
    {
        _items =
        [
            BlobsModelFactory.BlobItem(
                name: "test1.txt",
                deleted: false,
                snapshot: null,
                versionId: null,
                isLatestVersion: true,
                properties: BlobsModelFactory.BlobItemProperties(accessTierInferred: true)
            ),
            BlobsModelFactory.BlobItem(
                name: "test2.jpg",
                deleted: false,
                snapshot: null,
                versionId: null,
                isLatestVersion: false,
                properties: BlobsModelFactory.BlobItemProperties(accessTierInferred: true)
            )
        ];
    }

    public IAsyncEnumerator<BlobItem> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => GetAsyncEnumeratorImpl(cancellationToken);

    private async IAsyncEnumerator<BlobItem> GetAsyncEnumeratorImpl(CancellationToken _)
    {
        foreach (var item in _items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    public IAsyncEnumerable<Page<BlobItem>> AsPages(string? continuationToken = default, int? pageSizeHint = default)
        => AsPagesImpl(continuationToken, pageSizeHint);

    private async IAsyncEnumerable<Page<BlobItem>> AsPagesImpl(string? continuationToken, int? pageSizeHint)
    {
        var pageSize = pageSizeHint ?? _items.Count;
        var startIndex = 0;

        if (!string.IsNullOrEmpty(continuationToken) && int.TryParse(continuationToken, out var tokenIndex))
        {
            startIndex = tokenIndex;
        }

        while (startIndex < _items.Count)
        {
            var pageItems = _items.Skip(startIndex).Take(pageSize).ToList();
            var nextToken = (startIndex + pageItems.Count) < _items.Count
                ? (startIndex + pageItems.Count).ToString()
                : null;

            yield return Page<BlobItem>.FromValues(pageItems, nextToken, response: null!);
            await Task.Yield();

            startIndex += pageItems.Count;
            if (nextToken == null)
            {
                break;
            }
        }
    }
}