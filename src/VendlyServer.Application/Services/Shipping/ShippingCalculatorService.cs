using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.BtsExpress;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Requests;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;

namespace VendlyServer.Application.Services.Shipping;

public class ShippingCalculatorService(
    IBtsBroker btsBroker,
    IOptions<BtsExpressOptions> options) : IShippingCalculatorService
{
    private const string Currency = "UZS";
    private readonly BtsExpressOptions _options = options.Value;

    public async Task<Result<ShippingQuoteResponse>> CalculateAsync(
        ShippingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        if (request.WeightKg <= 0)
            return ShippingErrors.WeightMissing;

        // Manzilda BTS filial kodi bo'lsa filialga, bo'lmasa kuryer orqali yetkaziladi.
        var dropoffType = string.IsNullOrWhiteSpace(request.ReceiverBranchCode) ? "courier" : "branch";

        // Test/stage override — agar config'da FixedQuoteAmount belgilangan bo'lsa,
        // BTS chaqirilmasdan o'sha narx qaytariladi. Production'da bu NULL bo'ladi.
        if (_options.FixedQuoteAmount is > 0)
            return new ShippingQuoteResponse(_options.FixedQuoteAmount.Value, dropoffType, Currency);

        var calcRequest = new BtsCalculateRequest
        {
            SenderCityCode = _options.SenderCityCode,
            ReceiverCityCode = request.ReceiverCityCode,
            PickupType = _options.DefaultPickupType,
            DropoffType = dropoffType,
            IsMultipleCost = 1,
            Weight = request.WeightKg,
            Volume = null,
        };

        var calcResult = await btsBroker.CalculateAsync(calcRequest, cancellationToken);
        if (calcResult.IsFailure || calcResult.Data is null)
            return ShippingErrors.CalculateFailed;

        var option = SelectOption(calcResult.Data, _options.DefaultPickupType, dropoffType);
        if (option is null || !option.Available)
            return ShippingErrors.RouteUnavailable;

        return new ShippingQuoteResponse(option.Price, dropoffType, Currency);
    }

    private static BtsPriceOption? SelectOption(BtsCalculateData data, string pickupType, string dropoffType) =>
        (pickupType, dropoffType) switch
        {
            ("branch", "courier") => data.BranchToCourier,
            ("branch", "branch") => data.BranchToBranch,
            ("courier", "courier") => data.CourierToCourier,
            ("courier", "branch") => data.CourierToBranch,
            _ => data.BranchToCourier,
        };
}
