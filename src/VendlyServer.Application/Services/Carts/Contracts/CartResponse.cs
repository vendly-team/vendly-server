namespace VendlyServer.Application.Services.Carts.Contracts;

public record CartResponse(
    long Id,
    List<CartItemResponse> Items,
    decimal TotalAmount,
    bool IsLocked = false);
