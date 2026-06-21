using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Catalog;
using VendlyServer.Application.Services.RecentlyViewed;
using VendlyServer.Application.Services.RecentlyViewed.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class RecentlyViewedControllerTests
{
    private readonly FakeRecentlyViewedService _svc = new();

    private RecentlyViewedController CreateController(long userId = 1)
    {
        var ctrl = new RecentlyViewedController(_svc);
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

    // ── GetAll ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        var items = new List<RecentlyViewedResponse> { new(1, 10, DateTime.UtcNow) };
        _svc.GetAllResult = items;

        var result = await CreateController().GetAllAsync();

        var ok = Assert.IsType<Ok<List<RecentlyViewedResponse>>>(result);
        Assert.Equal(items, ok.Value);
    }

    [Fact]
    public async Task GetAll_ReturnsProblem_OnFailure()
    {
        _svc.GetAllError = RecentlyViewedErrors.NotFound;

        var result = await CreateController().GetAllAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Track ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Track_ReturnsOk_OnSuccess()
    {
        _svc.TrackSuccess = true;

        var result = await CreateController().TrackAsync(new TrackProductViewRequest(10));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Track_ReturnsProblem_OnProductNotFound()
    {
        _svc.TrackError = RecentlyViewedErrors.ProductNotFound;

        var result = await CreateController().TrackAsync(new TrackProductViewRequest(999));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Sync ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sync_ReturnsOk_OnSuccess()
    {
        _svc.SyncSuccess = true;

        var result = await CreateController().SyncAsync(new BulkSyncRecentlyViewedRequest([1, 2, 3]));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Sync_ReturnsProblem_OnFailure()
    {
        _svc.SyncError = RecentlyViewedErrors.ProductNotFound;

        var result = await CreateController().SyncAsync(new BulkSyncRecentlyViewedRequest([1]));

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Clear ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Clear_ReturnsOk_OnSuccess()
    {
        _svc.ClearSuccess = true;

        var result = await CreateController().ClearAsync();

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Clear_ReturnsProblem_OnFailure()
    {
        _svc.ClearError = RecentlyViewedErrors.NotFound;

        var result = await CreateController().ClearAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Fake service ────────────────────────────────────────────────────────

    private class FakeRecentlyViewedService : IRecentlyViewedService
    {
        public List<RecentlyViewedResponse>? GetAllResult;
        public Error? GetAllError;
        public bool TrackSuccess;
        public Error? TrackError;
        public bool SyncSuccess;
        public Error? SyncError;
        public bool ClearSuccess;
        public Error? ClearError;

        public Task<Result<List<RecentlyViewedResponse>>> GetAllAsync(long userId, CancellationToken ct = default)
            => Task.FromResult(GetAllError is { } e
                ? Result<List<RecentlyViewedResponse>>.Failure(e)
                : Result<List<RecentlyViewedResponse>>.Success(GetAllResult ?? []));

        public Task<Result> TrackAsync(long userId, TrackProductViewRequest request, CancellationToken ct = default)
            => Task.FromResult(TrackError is { } e ? Result.Failure(e) : Result.Success());

        public Task<Result> BulkSyncAsync(long userId, BulkSyncRecentlyViewedRequest request, CancellationToken ct = default)
            => Task.FromResult(SyncError is { } e ? Result.Failure(e) : Result.Success());

        public Task<Result> ClearAsync(long userId, CancellationToken ct = default)
            => Task.FromResult(ClearError is { } e ? Result.Failure(e) : Result.Success());
    }
}
