using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using VendlyServer.Api.Controllers.Common;
using VendlyServer.Application.Services.Users;
using VendlyServer.Application.Services.Users.Contracts;

namespace VendlyServer.Api.Controllers.Public;

[Route("api/me")]
public class MeController(IUserService userService, IMemoryCache cache) : AuthorizedController
{
    /// <summary>
    /// Get current user's profile with orders and reviews. Cached 30 minutes per user.
    /// </summary>
    [HttpGet]
    public async Task<IResult> GetAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"user_me:{UserId}";

        if (cache.TryGetValue(cacheKey, out UserDetailResponse? cached))
            return Results.Ok(cached);

        var result = await userService.GetByIdAsync(UserId, cancellationToken);

        if (!result.IsSuccess)
            return result.ToProblemDetails();

        cache.Set(cacheKey, result.Data, TimeSpan.FromMinutes(30));

        return Results.Ok(result.Data);
    }
}
