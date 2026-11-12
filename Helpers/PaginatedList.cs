using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Helpers;

public sealed class PaginatedList<T>
{
    private PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        Items = items;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
    }

    public List<T> Items { get; }

    public int PageIndex { get; }

    public int TotalPages { get; }

    public int TotalCount { get; }

    public int PageSize { get; }

    public bool HasPreviousPage => PageIndex > 1;

    public bool HasNextPage => PageIndex < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageIndex,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var count = await source.CountAsync();
        var items = await source
            .Skip(checked((pageIndex - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}
