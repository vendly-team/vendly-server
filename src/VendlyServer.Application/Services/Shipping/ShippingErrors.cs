using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Shipping;

public static class ShippingErrors
{
    public static readonly Error WeightMissing = Error.Validation(
        "Shipping.WeightMissing", "One or more products are missing a weight.");

    public static readonly Error RouteUnavailable = Error.Validation(
        "Shipping.RouteUnavailable", "Delivery is not available for this route.");

    public static readonly Error CalculateFailed = Error.Failure("Shipping.CalculateFailed");
}
