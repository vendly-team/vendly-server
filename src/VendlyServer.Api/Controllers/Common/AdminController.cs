using Microsoft.AspNetCore.Authorization;

namespace VendlyServer.Api.Controllers.Common;

[Authorize(Roles = "Admin,Manager")]
public abstract class AdminController : AuthorizedController
{
}
