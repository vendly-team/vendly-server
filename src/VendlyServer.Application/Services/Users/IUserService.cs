using VendlyServer.Application.Services.Users.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Users;

public interface IUserService
{
    Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<UserDetailResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(long id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> BlockAsync(long id, UserRole callerRole, CancellationToken cancellationToken = default);
    Task<Result> AssignRoleAsync(long id, AssignRoleRequest request, CancellationToken cancellationToken = default);
}
