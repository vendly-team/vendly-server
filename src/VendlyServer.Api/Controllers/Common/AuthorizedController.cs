using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Authentication;

namespace VendlyServer.Api.Controllers.Common;

[ApiController]
[Authorize]
public abstract class AuthorizedController : ControllerBase
{
    protected long UserId
    {
        get
        {
            var raw = HttpContext.User.FindFirstValue(CustomClaims.Id)
                      ?? throw new UnauthorizedAccessException("Required claim not found");
            return (long)Convert.ChangeType(raw, typeof(long));
        }
    }

    protected UserRole Role => Enum.Parse<UserRole>(
        HttpContext.User.FindFirstValue(CustomClaims.Role)
            ?? throw new UnauthorizedAccessException("Role claim not found"),
        ignoreCase: true);
}
