using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Addresses;
using VendlyServer.Application.Services.Addresses.Contracts;

namespace VendlyServer.Api.Controllers.Public;

[Route("api/addresses")]
public class AddressesController(IAddressService addressService) : AuthorizedController
{
    /// <summary>Get all addresses of the current user.</summary>
    [HttpGet]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await addressService.GetAllForUserAsync(UserId, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Get a single address by id (current user).</summary>
    [HttpGet("{id:long}")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await addressService.GetByIdAsync(UserId, id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Create a new address for the current user.</summary>
    [HttpPost]
    public async Task<IResult> AddAsync(
        [FromBody] CreateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await addressService.AddAsync(UserId, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Update an existing address.</summary>
    [HttpPut("{id:long}")]
    public async Task<IResult> UpdateAsync(
        long id,
        [FromBody] UpdateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await addressService.UpdateAsync(UserId, id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>Soft-delete an address.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await addressService.DeleteAsync(UserId, id, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>Mark an address as the user's default delivery address.</summary>
    [HttpPut("{id:long}/set-default")]
    public async Task<IResult> SetDefaultAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await addressService.SetDefaultAsync(UserId, id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }
}
