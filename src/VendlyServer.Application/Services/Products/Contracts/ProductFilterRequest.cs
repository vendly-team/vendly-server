using System.ComponentModel.DataAnnotations;

namespace VendlyServer.Application.Services.Products.Contracts;

public record ProductFilterRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public long? CategoryId { get; init; }
}
