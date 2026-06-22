using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.CategoryPrices;
using VendlyServer.Application.Services.CategoryPrices.Contracts;

namespace VendlyServer.Api.Controllers.Catalog;

[Route("api/category-prices")]
[Authorize(Roles = "Admin,Manager")]
public class CategoryPricesController(ICategoryPriceService categoryPriceService) : AuthorizedController
{
    /// <summary>Get all category prices. Optional categoryId filter.</summary>
    [HttpGet]
    public async Task<IResult> GetAllAsync([FromQuery] long? categoryId, CancellationToken cancellationToken = default)
    {
        var result = await categoryPriceService.GetAllAsync(categoryId, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get category price by id.</summary>
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await categoryPriceService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create a category price rule. Returns the new id.</summary>
    [HttpPost]
    public async Task<IResult> AddAsync(
        [FromBody] CreateCategoryPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await categoryPriceService.AddAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Update a category price rule.</summary>
    [HttpPut("{id:long}")]
    public async Task<IResult> UpdateAsync(
        long id,
        [FromBody] UpdateCategoryPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await categoryPriceService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Delete a category price rule.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await categoryPriceService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
