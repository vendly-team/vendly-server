using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Users.Contracts;

public record UserOrderSummary(
    long Id,
    string OrderNumber,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt);
