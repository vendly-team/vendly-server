using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Sms;
using VendlyServer.Application.Services.Sms.Contracts;
using VendlyServer.Infrastructure.Extensions;

namespace VendlyServer.Api.Controllers.Admin;

[ApiController]
[Route("api/sms")]
public class SmsController(ISmsService smsService) : AdminController
{
    /// <summary>
    /// Send a single SMS via Eskiz.uz.
    /// </summary>
    [HttpPost("send")]
    public async Task<IResult> SendAsync(
        [FromBody] SendSmsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await smsService.SendAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Get the current Eskiz.uz account balance.
    /// </summary>
    [HttpGet("balance")]
    public async Task<IResult> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var result = await smsService.GetBalanceAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Get an SMS log entry by id.
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await smsService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }
}
