namespace VendlyServer.Application.Services.Carts.Contracts;

public record CartItemRequest(long ProductVariantId, int Qty);
