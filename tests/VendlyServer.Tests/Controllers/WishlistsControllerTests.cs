using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Catalog;
using VendlyServer.Application.Services.Wishlist;
using VendlyServer.Application.Services.Wishlist.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class WishlistsControllerTests
{
    private readonly FakeWishlistService _svc = new();

    private WishlistsController CreateController(long userId = 1)
    {
        var ctrl = new WishlistsController(_svc);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("user_id", userId.ToString())]))
            }
        };
        return ctrl;
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        var items = new List<WishlistResponse> { new(1, 10, DateTime.UtcNow) };
        _svc.GetAllResult = items;

        var result = await CreateController().GetAllAsync();

        var ok = Assert.IsType<Ok<List<WishlistResponse>>>(result);
        Assert.Equal(items, ok.Value);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        var item = new WishlistResponse(1, 10, DateTime.UtcNow);
        _svc.GetByIdResult = item;

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<WishlistResponse>>(result);
        Assert.Equal(item, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetByIdError = WishlistErrors.NotFound;

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_ReturnsOk_OnSuccess()
    {
        _svc.AddSuccess = true;

        var result = await CreateController().AddAsync(new AddWishlistRequest(10));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnAlreadyExists()
    {
        _svc.AddError = WishlistErrors.AlreadyExists;

        var result = await CreateController().AddAsync(new AddWishlistRequest(10));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ReturnsOk_OnSuccess()
    {
        _svc.DeleteSuccess = true;

        var result = await CreateController().DeleteAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Delete_ReturnsProblem_OnNotFound()
    {
        _svc.DeleteError = WishlistErrors.NotFound;

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Fake service ──────────────────────────────────────────────────────────

    private class FakeWishlistService : IWishlistService
    {
        public List<WishlistResponse>? GetAllResult;
        public WishlistResponse? GetByIdResult;
        public Error? GetByIdError;
        public bool AddSuccess;
        public Error? AddError;
        public bool DeleteSuccess;
        public Error? DeleteError;

        public Task<Result<List<WishlistResponse>>> GetAllAsync(long userId, CancellationToken ct = default)
            => Task.FromResult<Result<List<WishlistResponse>>>(GetAllResult ?? []);

        public Task<Result<WishlistResponse>> GetByIdAsync(long id, long userId, CancellationToken ct = default)
            => Task.FromResult(GetByIdError is { } e
                ? Result<WishlistResponse>.Failure(e)
                : Result<WishlistResponse>.Success(GetByIdResult!));

        public Task<Result> AddAsync(long userId, AddWishlistRequest request, CancellationToken ct = default)
            => Task.FromResult(AddError is { } e ? Result.Failure(e) : Result.Success());

        public Task<Result> DeleteAsync(long id, long userId, CancellationToken ct = default)
            => Task.FromResult(DeleteError is { } e ? Result.Failure(e) : Result.Success());
    }
}
