using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.CompanyInfo;
using VendlyServer.Application.Services.CompanyInfo.Contracts;

namespace VendlyServer.Api.Controllers.Public;

[Route("api/company-info")]
public class CompanyInfoController(ICompanyInfoService companyInfoService) : AuthorizedController
{
    /// <summary>Get company info and oferta (PDF) links. Oferta URL follows Accept-Language.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IResult> GetAsync(CancellationToken cancellationToken = default)
    {
        var result = await companyInfoService.GetAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create or update company info. Accepts logo image and per-language oferta PDF files.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> UpsertAsync(
        [FromForm] UpsertCompanyInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await companyInfoService.UpsertAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }
}
