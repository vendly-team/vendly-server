using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Application.Services.Currency;
using VendlyServer.Infrastructure.Extensions;

namespace VendlyServer.Api.Controllers.Public;

[ApiController]
[Route("api/currency")]
public class CurrencyController(ICurrencyConverterService currencyConverterService) : ControllerBase
{
    /// <summary>Convert an amount from one currency to another.</summary>
    [AllowAnonymous]
    [HttpGet("convert")]
    public async Task<IResult> ConvertAsync(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] decimal amount,
        CancellationToken cancellationToken = default)
    {
        var result = await currencyConverterService.ConvertAsync(from, to, amount, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }
}
