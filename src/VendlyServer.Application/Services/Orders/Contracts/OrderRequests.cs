namespace VendlyServer.Application.Services.Orders.Contracts;

public record OrderFilterRequest
{
    public string? Status { get; init; }
    public string? Search { get; init; }
}

public record CreateOrderRequest(long AddressId);

public record SetOrderAddressRequest(long AddressId);

public record UpdateOrderStatusRequest(string Status, string? Note);

public record AddOrderNoteRequest(string Note);

public record CancelOrderRequest(string? ReasonCode, string? ReasonText);
