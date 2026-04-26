using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Category;
using VendlyServer.Application.Services.Category.Contracts;
using VendlyServer.Infrastructure.Extensions;

namespace VendlyServer.Api.Controllers.Catalog;

[Route("api/categories")]
public class CategoriesController(ICategoryService categoryService) : AuthorizedController
{
    /// <summary>Get all categories.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await categoryService.GetAllAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get category by id.</summary>
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await categoryService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create new category.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IResult> AddAsync(
        [FromForm] CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await categoryService.AddAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Update category.</summary>
    [HttpPut("{id:long}")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> UpdateAsync(
        long id,
        [FromForm] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await categoryService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Delete category.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await categoryService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Toggle category active/inactive status.</summary>
    [HttpPatch("{id:long}/toggle")]
    public async Task<IResult> ToggleActiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await categoryService.ToggleActiveAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
