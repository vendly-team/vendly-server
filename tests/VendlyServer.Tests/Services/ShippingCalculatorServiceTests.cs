using Microsoft.Extensions.Options;
using VendlyServer.Application.Services.Shipping;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.BtsExpress;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Requests;
using VendlyServer.Infrastructure.Brokers.BtsExpress.Contracts.Responses;

namespace VendlyServer.Tests.Services;

public class ShippingCalculatorServiceTests
{
    private readonly FakeBtsBroker _broker = new();
    private readonly ShippingCalculatorService _service;

    public ShippingCalculatorServiceTests()
    {
        var options = Options.Create(new BtsExpressOptions
        {
            SenderCityCode = "TASH",
            DefaultPickupType = "branch",
        });

        _service = new ShippingCalculatorService(_broker, options);
    }

    [Fact]
    public async Task Calculate_ReturnsWeightMissing_WhenWeightIsZero()
    {
        var result = await _service.CalculateAsync(new ShippingQuoteRequest("CITY", null, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(ShippingErrors.WeightMissing, result.Error);
    }

    [Fact]
    public async Task Calculate_ReturnsWeightMissing_WhenWeightIsNegative()
    {
        var result = await _service.CalculateAsync(new ShippingQuoteRequest("CITY", null, -1.5));

        Assert.False(result.IsSuccess);
        Assert.Equal(ShippingErrors.WeightMissing, result.Error);
    }

    [Fact]
    public async Task Calculate_UsesCourierDropoff_WhenBranchCodeMissing()
    {
        _broker.CalculateResult = Result<BtsCalculateData>.Success(new BtsCalculateData
        {
            BranchToCourier = new BtsPriceOption { Available = true, Price = 25_000m },
        });

        var result = await _service.CalculateAsync(new ShippingQuoteRequest("CITY", null, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(25_000m, result.Data!.Cost);
        Assert.Equal("courier", result.Data.DropoffType);
        Assert.Equal("UZS", result.Data.Currency);
        Assert.Equal("courier", _broker.LastRequest!.DropoffType);
        Assert.Equal("branch", _broker.LastRequest.PickupType);
        Assert.Equal("TASH", _broker.LastRequest.SenderCityCode);
        Assert.Equal("CITY", _broker.LastRequest.ReceiverCityCode);
    }

    [Fact]
    public async Task Calculate_UsesCourierDropoff_WhenBranchCodeIsWhitespace()
    {
        _broker.CalculateResult = Result<BtsCalculateData>.Success(new BtsCalculateData
        {
            BranchToCourier = new BtsPriceOption { Available = true, Price = 10_000m },
        });

        var result = await _service.CalculateAsync(new ShippingQuoteRequest("CITY", "  ", 1));

        Assert.True(result.IsSuccess);
        Assert.Equal("courier", result.Data!.DropoffType);
    }

    [Fact]
    public async Task Calculate_UsesBranchDropoff_WhenBranchCodePresent()
    {
        _broker.CalculateResult = Result<BtsCalculateData>.Success(new BtsCalculateData
        {
            BranchToBranch = new BtsPriceOption { Available = true, Price = 18_000m },
        });

        var result = await _service.CalculateAsync(new ShippingQuoteRequest("CITY", "BR1", 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(18_000m, result.Data!.Cost);
        Assert.Equal("branch", result.Data.DropoffType);
        Assert.Equal("branch", _broker.LastRequest!.DropoffType);
    }

    [Fact]
    public async Task Calculate_AllowsZeroPrice()
    {
        _broker.CalculateResult = Result<BtsCalculateData>.Success(new BtsCalculateData
        {
            BranchToCourier = new BtsPriceOption { Available = true, Price = 0m },
        });

        var result = await _service.CalculateAsync(new ShippingQuoteRequest("CITY", null, 0.01));

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Data!.Cost);
    }

    [Fact]
    public async Task Calculate_ReturnsCalculateFailed_WhenBrokerFails()
    {
        _broker.CalculateResult = Result<BtsCalculateData>.Failure(Error.Failure("Bts.Boom"));

        var result = await _service.CalculateAsync(new ShippingQuoteRequest("CITY", null, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ShippingErrors.CalculateFailed, result.Error);
    }

    [Fact]
    public async Task Calculate_ReturnsCalculateFailed_WhenBrokerDataNull()
    {
        _broker.CalculateResult = Result<BtsCalculateData>.Success(null!);

        var result = await _service.CalculateAsync(new ShippingQuoteRequest("CITY", null, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ShippingErrors.CalculateFailed, result.Error);
    }

    [Fact]
    public async Task Calculate_ReturnsRouteUnavailable_WhenOptionIsNull()
    {
        // pickup=branch, dropoff=courier selects BranchToCourier, which is null here.
        _broker.CalculateResult = Result<BtsCalculateData>.Success(new BtsCalculateData());

        var result = await _service.CalculateAsync(new ShippingQuoteRequest("CITY", null, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ShippingErrors.RouteUnavailable, result.Error);
    }

    [Fact]
    public async Task Calculate_ReturnsRouteUnavailable_WhenOptionNotAvailable()
    {
        _broker.CalculateResult = Result<BtsCalculateData>.Success(new BtsCalculateData
        {
            BranchToCourier = new BtsPriceOption { Available = false, Price = 5_000m },
        });

        var result = await _service.CalculateAsync(new ShippingQuoteRequest("CITY", null, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ShippingErrors.RouteUnavailable, result.Error);
    }

    [Fact]
    public async Task Calculate_PassesWeightToBroker()
    {
        _broker.CalculateResult = Result<BtsCalculateData>.Success(new BtsCalculateData
        {
            BranchToCourier = new BtsPriceOption { Available = true, Price = 1m },
        });

        await _service.CalculateAsync(new ShippingQuoteRequest("CITY", null, 7.5));

        Assert.Equal(7.5, _broker.LastRequest!.Weight);
        Assert.Equal(1, _broker.LastRequest.IsMultipleCost);
    }

    [Fact]
    public async Task Calculate_SelectsCourierToCourier_WhenPickupCourierAndNoBranch()
    {
        var options = Options.Create(new BtsExpressOptions
        {
            SenderCityCode = "TASH",
            DefaultPickupType = "courier",
        });
        var service = new ShippingCalculatorService(_broker, options);

        _broker.CalculateResult = Result<BtsCalculateData>.Success(new BtsCalculateData
        {
            CourierToCourier = new BtsPriceOption { Available = true, Price = 33_000m },
        });

        var result = await service.CalculateAsync(new ShippingQuoteRequest("CITY", null, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(33_000m, result.Data!.Cost);
    }

    [Fact]
    public async Task Calculate_SelectsCourierToBranch_WhenPickupCourierAndBranchPresent()
    {
        var options = Options.Create(new BtsExpressOptions
        {
            SenderCityCode = "TASH",
            DefaultPickupType = "courier",
        });
        var service = new ShippingCalculatorService(_broker, options);

        _broker.CalculateResult = Result<BtsCalculateData>.Success(new BtsCalculateData
        {
            CourierToBranch = new BtsPriceOption { Available = true, Price = 44_000m },
        });

        var result = await service.CalculateAsync(new ShippingQuoteRequest("CITY", "BR9", 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(44_000m, result.Data!.Cost);
        Assert.Equal("branch", result.Data.DropoffType);
    }
}
