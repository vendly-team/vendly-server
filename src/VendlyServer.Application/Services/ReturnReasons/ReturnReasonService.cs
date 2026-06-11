using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.ReturnReasons.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Orders;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.ReturnReasons;

public class ReturnReasonService(AppDbContext dbContext) : IReturnReasonService
{
    public async Task<Result<List<ReturnReasonResponse>>> GetAllAsync(ReturnReasonFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var reasons = await dbContext.ReturnReasons
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Where(r => filter.Search == null || r.Key.Contains(filter.Search) || r.Name.Uz!.Contains(filter.Search) || r.Name.Ru!.Contains(filter.Search))
            .ToListAsync(cancellationToken);

        return reasons
            .Select(r => new ReturnReasonResponse(r.Id, r.Key, r.Name, r.Description, r.CanResell, r.CreatedAt))
            .ToList();
    }

    public async Task<Result<ReturnReasonResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var reason = await dbContext.ReturnReasons
            .AsNoTracking()
            .Where(r => r.Id == id && !r.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        return reason is null
            ? ReturnReasonErrors.NotFound
            : new ReturnReasonResponse(reason.Id, reason.Key, reason.Name, reason.Description, reason.CanResell, reason.CreatedAt);
    }

    public async Task<Result> AddAsync(CreateReturnReasonRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.ReturnReasons
            .AnyAsync(r => r.Key == request.Key && !r.IsDeleted, cancellationToken);

        if (exists) return ReturnReasonErrors.KeyExists;

        dbContext.ReturnReasons.Add(new ReturnReason
        {
            Key         = request.Key,
            Name        = request.Name,
            Description = request.Description,
            CanResell   = request.CanResell,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(long id, CreateReturnReasonRequest request, CancellationToken cancellationToken = default)
    {
        var reason = await dbContext.ReturnReasons
            .SingleOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (reason is null) return ReturnReasonErrors.NotFound;

        var keyExists = await dbContext.ReturnReasons
            .AnyAsync(r => r.Key == request.Key && r.Id != id && !r.IsDeleted, cancellationToken);

        if (keyExists) return ReturnReasonErrors.KeyExists;

        reason.Key         = request.Key;
        reason.Name        = request.Name;
        reason.Description = request.Description;
        reason.CanResell   = request.CanResell;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var reason = await dbContext.ReturnReasons
            .SingleOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (reason is null) return ReturnReasonErrors.NotFound;

        reason.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
