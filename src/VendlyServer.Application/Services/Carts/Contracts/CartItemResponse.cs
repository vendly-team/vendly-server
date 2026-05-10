namespace VendlyServer.Application.Services.Carts.Contracts;

public record CartItemResponse(
    long Id,
    long ProductVariantId,
    long ProductId,
    string ProductName,
    string? VariantName,
    decimal Price,
    List<string> Images,
    int Qty,
    int Stock);
