using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Orders;
using VendlyServer.Application.Services.Shipping;
using VendlyServer.Application.Services.Shipping.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class ShippingControllerTests
{
    private readonly FakeShippingCalculatorService _svc = new();

    private ShippingController CreateController(long userId = 1)
    {
        var ctrl = new ShippingController(_svc);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("user_id", userId.ToString())]))
            }
        };
        return ctrl;
    }

    [Fact]
    public async Task Quote_ReturnsOkWithData_OnSuccess()
    {
        var quote = new ShippingQuoteResponse(25_000m, "courier", "UZS");
        _svc.CalculateResult = Result<ShippingQuoteResponse>.Success(quote);

        var result = await CreateController().QuoteAsync(new ShippingQuoteRequest("CITY", null, 2));

        var ok = Assert.IsType<Ok<ShippingQuoteResponse>>(result);
        Assert.Equal(quote, ok.Value);
    }

    [Fact]
    public async Task Quote_ReturnsProblem_OnWeightMissing()
    {
        _svc.CalculateResult = Result<ShippingQuoteResponse>.Failure(ShippingErrors.WeightMissing);

        var result = await CreateController().QuoteAsync(new ShippingQuoteRequest("CITY", null, 0));

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Quote_ReturnsProblem_OnRouteUnavailable()
    {
        _svc.CalculateResult = Result<ShippingQuoteResponse>.Failure(ShippingErrors.RouteUnavailable);

        var result = await CreateController().QuoteAsync(new ShippingQuoteRequest("CITY", "BR1", 2));

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Quote_ReturnsProblem_OnCalculateFailed()
    {
        _svc.CalculateResult = Result<ShippingQuoteResponse>.Failure(ShippingErrors.CalculateFailed);

        var result = await CreateController().QuoteAsync(new ShippingQuoteRequest("CITY", null, 2));

        Assert.IsType<ProblemHttpResult>(result);
    }

    private sealed class FakeShippingCalculatorService : IShippingCalculatorService
    {
        public Result<ShippingQuoteResponse> CalculateResult { get; set; } =
            Result<ShippingQuoteResponse>.Success(new ShippingQuoteResponse(0m, "courier", "UZS"));

        public Task<Result<ShippingQuoteResponse>> CalculateAsync(
            ShippingQuoteRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(CalculateResult);
    }
}
