using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Users;
using VendlyServer.Application.Services.Users.Contracts;

namespace VendlyServer.Api.Controllers.Admin;

[Route("api/users")]
public class UsersController(IUserService userService) : AuthorizedController
{
    /// <summary>
    /// Get all users.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await userService.GetAllAsync(cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Get user by id with orders and reviews.
    /// </summary>
    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await userService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Data) : result.ToProblemDetails();
    }

    /// <summary>
    /// Create a new user. Restores soft-deleted user if same phone exists.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> CreateAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Update user name, phone and email.
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> UpdateAsync(
        long id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Toggle block status. Admin blocks anyone; Manager blocks Customers only.
    /// </summary>
    [HttpPatch("{id:long}/block")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IResult> BlockAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await userService.BlockAsync(id, Role, cancellationToken);

        if (!result.IsSuccess && result.Error == UserErrors.Forbidden)
            return Results.Forbid();

        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Assign a role to a user. Admin only.
    /// </summary>
    [HttpPatch("{id:long}/assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IResult> AssignRoleAsync(
        long id,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.AssignRoleAsync(id, request, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
