using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Products;
using VendlyServer.Application.Services.Products.Contracts;

namespace VendlyServer.Api.Controllers.Catalog;

[Route("api/products")]
public class ProductsController(IProductService productService) : AuthorizedController
{
    /// <summary>Get all products.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IResult> GetAllAsync(CancellationToken ct = default)
    {
        var result = await productService.GetAllAsync(ct);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get product detail with variant types, options, and variants.</summary>
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var result = await productService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create new product. Returns the new product id.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> CreateAsync([FromBody] CreateProductRequest request, CancellationToken ct = default)
    {
        var result = await productService.CreateAsync(request, ct);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Update product metadata.</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> UpdateAsync(long id, [FromBody] UpdateProductRequest request, CancellationToken ct = default)
    {
        var result = await productService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Soft-delete a product.</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var result = await productService.DeleteAsync(id, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Toggle product active/inactive.</summary>
    [HttpPatch("{id:long}/toggle")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        var result = await productService.ToggleActiveAsync(id, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
 
    /// <summary>Add a variant type (e.g. "Color", "Size") to a product.</summary>
    [HttpPost("{productId:long}/variant-types")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> AddVariantTypeAsync(long productId, [FromBody] CreateVariantTypeRequest request, CancellationToken ct = default)
    {
        var result = await productService.AddVariantTypeAsync(productId, request, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Delete a variant type.</summary>
    [HttpDelete("variant-types/{typeId:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> DeleteVariantTypeAsync(long typeId, CancellationToken ct = default)
    {
        var result = await productService.DeleteVariantTypeAsync(typeId, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Add an option to a variant type. Accepts optional image upload.</summary>
    [HttpPost("variant-types/{typeId:long}/options")]
    [Authorize(Roles = "Admin,Manager")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> AddVariantOptionAsync(long typeId, [FromForm] CreateVariantOptionRequest request, CancellationToken ct = default)
    {
        var result = await productService.AddVariantOptionAsync(typeId, request, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Delete a variant option.</summary>
    [HttpDelete("variant-options/{optionId:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> DeleteVariantOptionAsync(long optionId, CancellationToken ct = default)
    {
        var result = await productService.DeleteVariantOptionAsync(optionId, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Regenerate all SKU combinations from current variant types and options.</summary>
    [HttpPost("{productId:long}/regenerate-variants")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> RegenerateVariantsAsync(long productId, CancellationToken ct = default)
    {
        var result = await productService.RegenerateVariantsAsync(productId, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Bulk update multiple variants' price, quantity, active status, name, and optional image in one request.</summary>
    [HttpPut("{productId:long}/variants/bulk")]
    [Authorize(Roles = "Admin,Manager")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> BulkUpdateVariantsAsync(long productId, [FromForm] BulkUpdateVariantsRequest request, CancellationToken ct = default)
    {
        var result = await productService.BulkUpdateVariantsAsync(productId, request, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Update a variant's price, quantity, active status, and optional primary image.</summary>
    [HttpPut("variants/{variantId:long}")]
    [Authorize(Roles = "Admin,Manager")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> UpdateVariantAsync(long variantId, [FromForm] UpdateVariantRequest request, CancellationToken ct = default)
    {
        var result = await productService.UpdateVariantAsync(variantId, request, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Soft-delete a single variant.</summary>
    [HttpDelete("variants/{variantId:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> DeleteVariantAsync(long variantId, CancellationToken ct = default)
    {
        var result = await productService.DeleteVariantAsync(variantId, ct);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
