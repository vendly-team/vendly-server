namespace VendlyServer.Infrastructure.Authentication;

public interface IJwtProvider
{
    string GenerateToken(long userId, string email, string role);
}
