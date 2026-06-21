using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Application.Services.Users;
using VendlyServer.Application.Services.Users.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Authentication;

namespace VendlyServer.Tests.Controllers;

public class MeControllerTests
{
    private readonly FakeUserService _svc = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private static UserDetailResponse Sample(long id = 1) =>
        new(id, "Alice", "A", "111", null, UserRole.Customer, false, DateTime.UtcNow, null, [], []);

    private MeController CreateController(long userId = 1)
    {
        var ctrl = new MeController(_svc, _cache);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(CustomClaims.Id, userId.ToString())]))
            }
        };
        return ctrl;
    }

    [Fact]
    public async Task Get_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetByIdResult = Result<UserDetailResponse>.Success(Sample());

        var result = await CreateController().GetAsync();

        var ok = Assert.IsType<Ok<UserDetailResponse>>(result);
        Assert.Equal("Alice", ok.Value!.FirstName);
    }

    [Fact]
    public async Task Get_ReturnsProblem_WhenNotFound()
    {
        _svc.GetByIdResult = Result<UserDetailResponse>.Failure(UserErrors.NotFound);

        var result = await CreateController().GetAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Get_ServesFromCache_OnSecondCall()
    {
        _svc.GetByIdResult = Result<UserDetailResponse>.Success(Sample(7));

        var ctrl = CreateController(7);
        var first = await ctrl.GetAsync();
        Assert.IsType<Ok<UserDetailResponse>>(first);
        Assert.Equal(1, _svc.GetByIdCallCount);

        // Change the underlying result; cached value must still be returned.
        _svc.GetByIdResult = Result<UserDetailResponse>.Failure(UserErrors.NotFound);

        var second = await CreateController(7).GetAsync();

        var ok = Assert.IsType<Ok<UserDetailResponse>>(second);
        Assert.Equal(7, ok.Value!.Id);
        Assert.Equal(1, _svc.GetByIdCallCount);
    }

    [Fact]
    public async Task Get_DoesNotCache_OnFailure()
    {
        _svc.GetByIdResult = Result<UserDetailResponse>.Failure(UserErrors.NotFound);

        var first = await CreateController(9).GetAsync();
        Assert.IsType<ProblemHttpResult>(first);

        _svc.GetByIdResult = Result<UserDetailResponse>.Success(Sample(9));
        var second = await CreateController(9).GetAsync();

        Assert.IsType<Ok<UserDetailResponse>>(second);
        Assert.Equal(2, _svc.GetByIdCallCount);
    }

    private class FakeUserService : IUserService
    {
        public Result<UserDetailResponse> GetByIdResult { get; set; } =
            Result<UserDetailResponse>.Success(
                new(1, "T", "T", "000", null, UserRole.Customer, false, DateTime.UtcNow, null, [], []));
        public int GetByIdCallCount { get; private set; }

        public Task<Result<UserDetailResponse>> GetByIdAsync(long id, CancellationToken ct = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(GetByIdResult);
        }

        public Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Result<List<UserResponse>>.Success([]));
        public Task<Result> CreateAsync(CreateUserRequest r, CancellationToken ct = default)
            => Task.FromResult(Result.Success());
        public Task<Result> UpdateAsync(long id, UpdateUserRequest r, CancellationToken ct = default)
            => Task.FromResult(Result.Success());
        public Task<Result> BlockAsync(long id, UserRole callerRole, CancellationToken ct = default)
            => Task.FromResult(Result.Success());
        public Task<Result> AssignRoleAsync(long id, AssignRoleRequest r, CancellationToken ct = default)
            => Task.FromResult(Result.Success());
    }
}
