namespace VendlyServer.Domain.Abstractions;

public record DataQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
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
