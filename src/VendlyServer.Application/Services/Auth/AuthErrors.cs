using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials  = Error.Unauthorized("Auth.InvalidCredentials");
    public static readonly Error UserAlreadyExists   = Error.Conflict("Auth.UserAlreadyExists");
    public static readonly Error InvalidRefreshToken = Error.Unauthorized("Auth.InvalidRefreshToken");
    public static readonly Error UserBlocked         = Error.Unauthorized("Auth.UserBlocked");
    public static readonly Error UserNotFound        = Error.NotFound("Auth.UserNotFound");
}
