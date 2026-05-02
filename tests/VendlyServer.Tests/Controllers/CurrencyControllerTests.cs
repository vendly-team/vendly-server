using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Application.Services.Currency;
using VendlyServer.Application.Services.Currency.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class CurrencyControllerTests
{
    private readonly FakeCurrencyConverterService _service = new();

    private CurrencyController CreateController()
    {
        var controller = new CurrencyController(_service);
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

    private sealed class FakeCurrencyConverterService : ICurrencyConverterService
    {
        public Result<CurrencyConversionResponse> ConvertResult { get; set; } =
            Result<CurrencyConversionResponse>.Failure(CurrencyErrors.ProviderUnavailable);

        public Task<Result<CurrencyConversionResponse>> ConvertAsync(
            string fromCurrency,
            string toCurrency,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ConvertResult);
        }
    }
}
