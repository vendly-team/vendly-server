namespace VendlyServer.Domain.Abstractions;

public record DataQueryRequest
{
    private int _page = 1;
    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 100);
    }

    public string? SortBy { get; set; }
    public SortDirection SortDirection { get; set; } = SortDirection.Asc;
}

public enum SortDirection
{
    Asc = 0,
    Desc = 1
}

public class PagedList<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
