using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Auth;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = Error.Failure("Auth.InvalidCredentials");
    public static readonly Error UserAlreadyExists  = Error.Conflict("Auth.UserAlreadyExists");
    public static readonly Error InvalidRefreshToken = Error.Failure("Auth.InvalidRefreshToken");
    public static readonly Error UserBlocked        = Error.Failure("Auth.UserBlocked");
}
