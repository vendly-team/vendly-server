using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Users;

public static class UserErrors
{
    public static readonly Error NotFound      = Error.NotFound("User.NotFound");
    public static readonly Error AlreadyExists = Error.Conflict("User.AlreadyExists");
    public static readonly Error Forbidden     = Error.Failure("User.Forbidden");
}
