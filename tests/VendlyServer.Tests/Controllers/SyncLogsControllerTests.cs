using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Admin;
using VendlyServer.Application.Jobs.SmartupCatalog;
using VendlyServer.Application.Services.SyncLogs;
using VendlyServer.Application.Services.SyncLogs.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class SyncLogsControllerTests
{
    private readonly FakeSyncLogService _svc = new();

    private SyncLogsController CreateController()
    {
        var ctrl = new SyncLogsController(_svc);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    // ── GetAll ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedList()
    {
        var paged = new PagedList<SyncLogListItem>
        {
            Items = [new(1, "smartup", 1, 5, 2, 3, 0, 120, DateTime.UtcNow, DateTime.UtcNow, null)],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };
        _svc.GetAllResult = paged;

        var result = await CreateController().GetAllAsync(new DataQueryRequest());

        var ok = Assert.IsType<Ok<PagedList<SyncLogListItem>>>(result);
        Assert.Equal(1, ok.Value!.TotalCount);
    }

    // ── GetById ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        var detail = new SyncLogDetailResponse(1, "smartup", 1, 5, 2, 3, 0, 120,
            DateTime.UtcNow, DateTime.UtcNow, null, null, null, null);
        _svc.GetByIdResult = detail;

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<SyncLogDetailResponse>>(result);
        Assert.Equal(detail, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetByIdError = SyncLogErrors.NotFound;

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── TriggerSync ─────────────────────────────────────────────────────────

    [Fact]
    public void TriggerSync_EnqueuesJob_ReturnsAccepted()
    {
        var jobs = new FakeBackgroundJobClient();

        var result = CreateController().TriggerSync(jobs);

        Assert.IsType<Accepted>(result);
        Assert.True(jobs.EnqueueCalled);
    }

    // ── Fakes ───────────────────────────────────────────────────────────────

    private class FakeSyncLogService : ISyncLogService
    {
        public PagedList<SyncLogListItem> GetAllResult { get; set; } = new();
        public SyncLogDetailResponse? GetByIdResult;
        public Error? GetByIdError;

        public Task<PagedList<SyncLogListItem>> GetAllAsync(DataQueryRequest request, CancellationToken ct = default)
            => Task.FromResult(GetAllResult);

        public Task<Result<SyncLogDetailResponse>> GetByIdAsync(long id, CancellationToken ct = default)
            => Task.FromResult(GetByIdError is { } e
                ? Result<SyncLogDetailResponse>.Failure(e)
                : Result<SyncLogDetailResponse>.Success(GetByIdResult!));
    }

    private class FakeBackgroundJobClient : IBackgroundJobClient
    {
        public bool EnqueueCalled;

        public string Create(Hangfire.Common.Job job, Hangfire.States.IState state)
        {
            EnqueueCalled = true;
            return "job-id";
        }

        public bool ChangeState(string jobId, Hangfire.States.IState state, string expectedState) => true;
    }
}
