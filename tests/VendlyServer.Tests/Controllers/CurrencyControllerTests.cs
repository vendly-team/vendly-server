using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Application.Services.Currencies;
using VendlyServer.Application.Services.Currencies.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class CurrencyControllerTests
{
    private readonly FakeCurrencyConverterService _service = new();

    private CurrenciesController CreateController()
    {
        var controller = new CurrenciesController(_service);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    [Fact]
    public async Task Convert_Returns200_WhenSuccessful()
    {
        _service.ConvertResult = Result<CurrencyConversionResponse>.Success(
            new("USD", "UZS", 10m, 1m, 12_600m, 126_000m));

        var result = await CreateController().ConvertAsync("USD", "UZS", 10m);

        var ok = Assert.IsType<Ok<CurrencyConversionResponse>>(result);
        Assert.Equal(126_000m, ok.Value!.ConvertedAmount);
    }

    [Fact]
    public async Task Convert_ReturnsProblem_WhenServiceFails()
    {
        _service.ConvertResult = Result<CurrencyConversionResponse>.Failure(CurrencyErrors.InvalidAmount);

        var result = await CreateController().ConvertAsync("USD", "UZS", 0m);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task GetUsdRate_Returns200_WhenSuccessful()
    {
        _service.GetUsdRateResult = Result<CurrencyRateResponse>.Success(
            new(12049.44m, 52.23m, "08.06.2026"));

        var result = await CreateController().GetUsdRateAsync();

        var ok = Assert.IsType<Ok<CurrencyRateResponse>>(result);
        Assert.Equal(12049.44m, ok.Value!.Rate);
        Assert.Equal(52.23m, ok.Value.Diff);
        Assert.Equal("08.06.2026", ok.Value.Date);
    }

    [Fact]
    public async Task GetUsdRate_ReturnsProblem_WhenServiceFails()
    {
        _service.GetUsdRateResult = Result<CurrencyRateResponse>.Failure(CurrencyErrors.ProviderUnavailable);

        var result = await CreateController().GetUsdRateAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    private sealed class FakeCurrencyConverterService : ICurrencyConverterService
    {
        public Result<CurrencyConversionResponse> ConvertResult { get; set; } =
            Result<CurrencyConversionResponse>.Failure(CurrencyErrors.ProviderUnavailable);

        public Result<CurrencyRateResponse> GetUsdRateResult { get; set; } =
            Result<CurrencyRateResponse>.Failure(CurrencyErrors.ProviderUnavailable);

        public Task<Result<CurrencyConversionResponse>> ConvertAsync(
            string fromCurrency,
            string toCurrency,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ConvertResult);
        }

        public Task<Result<CurrencyRateResponse>> GetUsdRateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetUsdRateResult);
        }
    }
}
