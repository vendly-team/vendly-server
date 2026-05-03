namespace VendlyServer.Application.Jobs.Auth;

public interface ICleanExpiredRefreshTokensJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
