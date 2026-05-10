namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductFilterRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public long? CategoryId { get; init; }
}
