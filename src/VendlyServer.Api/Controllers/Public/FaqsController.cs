using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Faqs;
using VendlyServer.Application.Services.Faqs.Contracts;
using VendlyServer.Infrastructure.Extensions;

namespace VendlyServer.Api.Controllers.Public;

[ApiController]
[Route("api/faqs")]
public class FaqsController(IFaqService faqService) : AdminController
{
    /// <summary>
    /// Get all FAQs.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IResult> GetAllAsync([FromQuery] FaqFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var result = await faqService.GetAllAsync(filter, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Get FAQ by id.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await faqService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Create new FAQ.
    /// </summary>
    [HttpPost]
    public async Task<IResult> AddAsync([FromBody] CreateFaqRequest request, CancellationToken cancellationToken = default)
    {
        var result = await faqService.AddAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Update FAQ.
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IResult> UpdateAsync(long id, [FromBody] CreateFaqRequest request, CancellationToken cancellationToken = default)
    {
        var result = await faqService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Delete FAQ.
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await faqService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
