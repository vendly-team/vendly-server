using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Jobs.Auth;

public class CleanExpiredRefreshTokensJob(
    AppDbContext dbContext,
    ILogger<CleanExpiredRefreshTokensJob> logger) : ICleanExpiredRefreshTokensJob
{
    private const int RevokedRetentionDays = 7;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var revokedCutoff = now.AddDays(-RevokedRetentionDays);

        var deleted = await dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAt < now || (rt.IsRevoked && rt.UpdatedAt < revokedCutoff))
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation(
            "Refresh token cleanup: deleted {Count} expired/revoked tokens at {RunAt}",
            deleted,
            now);
    }
}
