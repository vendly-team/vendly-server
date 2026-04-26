using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VendlyServer.Application.Services.Users.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Authentication;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Users;

public class UserService(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IMemoryCache cache) : IUserService
{
    private static string MeCacheKey(long id) => $"user_me:{id}";

    public async Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .Select(u => new UserResponse(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Phone,
                u.Email,
                u.Role,
                u.IsBlocked,
                u.CreatedAt,
                u.UpdatedAt))
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task<Result<UserDetailResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == id && !u.IsDeleted)
            .Select(u => new UserDetailResponse(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Phone,
                u.Email,
                u.Role,
                u.IsBlocked,
                u.CreatedAt,
                u.UpdatedAt,
                u.Orders
                    .Where(o => !o.IsDeleted)
                    .Select(o => new UserOrderSummary(
                        o.Id,
                        o.OrderNumber,
                        o.Status,
                        o.TotalAmount,
                        o.CreatedAt))
                    .ToList(),
                u.Reviews
                    .Where(r => !r.IsDeleted)
                    .Select(r => new UserReviewSummary(
                        r.Id,
                        r.ProductId,
                        r.Rating,
                        r.Feedback,
                        r.CreatedAt))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
            return UserErrors.NotFound;

        return user;
    }

    public async Task<Result> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Phone == request.Phone, cancellationToken);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
                return UserErrors.AlreadyExists;

            existing.IsDeleted = false;
            existing.FirstName = request.FirstName;
            existing.LastName = request.LastName;
            existing.Email = request.Email;
            existing.PasswordHash = passwordHasher.Hash(request.Password);
            existing.Role = request.Role;
            existing.IsBlocked = false;

            cache.Remove(MeCacheKey(existing.Id));
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = request.Role
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(long id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user is null)
            return UserErrors.NotFound;

        if (request.Phone != user.Phone)
        {
            var phoneExists = await dbContext.Users
                .AnyAsync(u => u.Phone == request.Phone && !u.IsDeleted && u.Id != id, cancellationToken);

            if (phoneExists)
                return UserErrors.AlreadyExists;
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.Email = request.Email;

        cache.Remove(MeCacheKey(id));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> BlockAsync(long id, UserRole callerRole, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user is null)
            return UserErrors.NotFound;

        if (callerRole == UserRole.Manager && user.Role != UserRole.Customer)
            return UserErrors.Forbidden;

        user.IsBlocked = !user.IsBlocked;

        cache.Remove(MeCacheKey(id));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> AssignRoleAsync(long id, AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (user is null)
            return UserErrors.NotFound;

        user.Role = request.Role;

        cache.Remove(MeCacheKey(id));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
