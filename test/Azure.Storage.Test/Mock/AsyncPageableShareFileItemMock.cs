using Azure;
using Azure.Storage.Files.Shares.Models;
using GeoLibrary.ORiN3.Provider.Azure.Storage.O3Object.Interface;

namespace GeoLibrary.ORiN3.Provider.Azure.Storage.Test.Mock;

internal class AsyncPageableShareFileItemMock : IAsyncPageable<ShareFileItem>
{
    private readonly List<ShareFileItem> _items;

    public AsyncPageableShareFileItemMock(int itemCount)
    {
        var counter = 0;
        _items = new int[itemCount].Select(_ => FilesModelFactory.ShareFileItem(
            name: $"{++counter}.txt",
            isDirectory: false
        )).ToList();
    }

    public AsyncPageableShareFileItemMock()
    {
        _items =
        [
            FilesModelFactory.ShareFileItem(
                name: "test1.txt",
                isDirectory: false
            ),
            FilesModelFactory.ShareFileItem(
                name: "test2.jpg",
                isDirectory: false
            ),
            FilesModelFactory.ShareFileItem(
                name: "hogeDir",
                isDirectory: true
            )
        ];
    }

    public IAsyncEnumerator<ShareFileItem> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => GetAsyncEnumeratorImpl(cancellationToken);

    private async IAsyncEnumerator<ShareFileItem> GetAsyncEnumeratorImpl(CancellationToken _)
    {
        foreach (var item in _items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    public IAsyncEnumerable<Page<ShareFileItem>> AsPages(string? continuationToken = default, int? pageSizeHint = default)
        => AsPagesImpl(continuationToken, pageSizeHint);

    private async IAsyncEnumerable<Page<ShareFileItem>> AsPagesImpl(string? continuationToken, int? pageSizeHint)
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

            yield return Page<ShareFileItem>.FromValues(pageItems, nextToken, response: null!);
            await Task.Yield();

            startIndex += pageItems.Count;
            if (nextToken == null)
            {
                break;
            }
        }
    }
}