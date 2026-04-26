using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Admin;
using VendlyServer.Application.Services.Users;
using VendlyServer.Application.Services.Users.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Authentication;

namespace VendlyServer.Tests.Controllers;

public class UsersControllerTests
{
    private readonly FakeUserService _svc = new();

    private UsersController CreateController(UserRole callerRole = UserRole.Admin)
    {
        var ctrl = new UsersController(_svc);
        var identity = new ClaimsIdentity(
        [
            new Claim(CustomClaims.Id, "99"),
            new Claim(CustomClaims.Role, callerRole.ToString())
        ], "test");

        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return ctrl;
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200_WithUserList()
    {
        _svc.GetAllResult = Result<List<UserResponse>>.Success(
        [
            new(1, "Alice", "A", "111", null, UserRole.Customer, false, DateTime.UtcNow, null),
            new(2, "Bob",   "B", "222", null, UserRole.Admin,    false, DateTime.UtcNow, null)
        ]);

        var result = await CreateController().GetAllAsync();

        var ok = Assert.IsType<Ok<List<UserResponse>>>(result);
        Assert.Equal(2, ok.Value!.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsProblem_OnFailure()
    {
        _svc.GetAllResult = Result<List<UserResponse>>.Failure(UserErrors.NotFound);

        var result = await CreateController().GetAllAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Returns200_WhenFound()
    {
        _svc.GetByIdResult = Result<UserDetailResponse>.Success(
            new(1, "Alice", "A", "111", null, UserRole.Customer, false, DateTime.UtcNow, null, [], []));

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<UserDetailResponse>>(result);
        Assert.Equal("Alice", ok.Value!.FirstName);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_WhenNotFound()
    {
        _svc.GetByIdResult = Result<UserDetailResponse>.Failure(UserErrors.NotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns200_OnSuccess()
    {
        _svc.CreateResult = Result.Success();

        var result = await CreateController().CreateAsync(
            new CreateUserRequest("Alice", "A", "111", "pass", null, UserRole.Customer));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Create_ReturnsProblem_OnConflict()
    {
        _svc.CreateResult = Result.Failure(UserErrors.AlreadyExists);

        var result = await CreateController().CreateAsync(
            new CreateUserRequest("Alice", "A", "111", "pass", null, UserRole.Customer));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Returns200_OnSuccess()
    {
        _svc.UpdateResult = Result.Success();

        var result = await CreateController().UpdateAsync(1, new UpdateUserRequest("Alice", "A", "111", null));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_WhenNotFound()
    {
        _svc.UpdateResult = Result.Failure(UserErrors.NotFound);

        var result = await CreateController().UpdateAsync(999, new UpdateUserRequest("X", "Y", "000", null));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── BlockAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Block_Returns200_OnSuccess()
    {
        _svc.BlockResult = Result.Success();

        var result = await CreateController(UserRole.Admin).BlockAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Block_ReturnsForbid_WhenForbidden()
    {
        _svc.BlockResult = Result.Failure(UserErrors.Forbidden);

        var result = await CreateController(UserRole.Manager).BlockAsync(2);

        Assert.IsType<ForbidHttpResult>(result);
    }

    [Fact]
    public async Task Block_ReturnsProblem_WhenNotFound()
    {
        _svc.BlockResult = Result.Failure(UserErrors.NotFound);

        var result = await CreateController().BlockAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── AssignRoleAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AssignRole_Returns200_OnSuccess()
    {
        _svc.AssignRoleResult = Result.Success();

        var result = await CreateController().AssignRoleAsync(1, new AssignRoleRequest(UserRole.Manager));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task AssignRole_ReturnsProblem_WhenNotFound()
    {
        _svc.AssignRoleResult = Result.Failure(UserErrors.NotFound);

        var result = await CreateController().AssignRoleAsync(999, new AssignRoleRequest(UserRole.Manager));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Fake service ──────────────────────────────────────────────────────────

    private class FakeUserService : IUserService
    {
        public Result<List<UserResponse>> GetAllResult { get; set; } = Result<List<UserResponse>>.Success([]);
        public Result<UserDetailResponse> GetByIdResult { get; set; } = Result<UserDetailResponse>.Success(
            new(1, "T", "T", "000", null, UserRole.Customer, false, DateTime.UtcNow, null, [], []));
        public Result CreateResult { get; set; } = Result.Success();
        public Result UpdateResult { get; set; } = Result.Success();
        public Result BlockResult  { get; set; } = Result.Success();
        public Result AssignRoleResult { get; set; } = Result.Success();

        public Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(GetAllResult);

        public Task<Result<UserDetailResponse>> GetByIdAsync(long id, CancellationToken ct = default)
            => Task.FromResult(GetByIdResult);

        public Task<Result> CreateAsync(CreateUserRequest r, CancellationToken ct = default)
            => Task.FromResult(CreateResult);

        public Task<Result> UpdateAsync(long id, UpdateUserRequest r, CancellationToken ct = default)
            => Task.FromResult(UpdateResult);

        public Task<Result> BlockAsync(long id, UserRole callerRole, CancellationToken ct = default)
            => Task.FromResult(BlockResult);

        public Task<Result> AssignRoleAsync(long id, AssignRoleRequest r, CancellationToken ct = default)
            => Task.FromResult(AssignRoleResult);
    }
}
