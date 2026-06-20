using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Shipping;

public interface IShippingCalculatorService
{
    Task<Result<ShippingQuoteResponse>> CalculateAsync(
        ShippingQuoteRequest request, CancellationToken cancellationToken = default);
}
