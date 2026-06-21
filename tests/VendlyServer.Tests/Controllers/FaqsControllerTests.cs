using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Application.Services.Faqs;
using VendlyServer.Application.Services.Faqs.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Tests.Controllers;

public class FaqsControllerTests
{
    private readonly FakeFaqService _svc = new();

    private FaqsController CreateController()
    {
        var ctrl = new FaqsController(_svc);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    private static MultiLanguageField Ml(string v) => new() { Uz = v, Ru = v, En = v, Cyrl = v };

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetAllResult = Result<List<FaqResponse>>.Success(
        [
            new(1, Ml("Q1"), Ml("A1"), DateTime.UtcNow),
            new(2, Ml("Q2"), Ml("A2"), DateTime.UtcNow)
        ]);

        var result = await CreateController().GetAllAsync(new FaqFilterRequest(null));

        var ok = Assert.IsType<Ok<List<FaqResponse>>>(result);
        Assert.Equal(2, ok.Value!.Count);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetByIdResult = Result<FaqResponse>.Success(new(1, Ml("Q"), Ml("A"), DateTime.UtcNow));

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<FaqResponse>>(result);
        Assert.Equal(1, ok.Value!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetByIdResult = Result<FaqResponse>.Failure(FaqErrors.NotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsOk_OnSuccess()
    {
        _svc.AddResult = Result.Success();

        var result = await CreateController().AddAsync(new CreateFaqRequest(Ml("Q"), Ml("A")));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        _svc.UpdateResult = Result.Success();

        var result = await CreateController().UpdateAsync(1, new CreateFaqRequest(Ml("Q"), Ml("A")));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_OnNotFound()
    {
        _svc.UpdateResult = Result.Failure(FaqErrors.NotFound);

        var result = await CreateController().UpdateAsync(999, new CreateFaqRequest(Ml("Q"), Ml("A")));

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_OnSuccess()
    {
        _svc.DeleteResult = Result.Success();

        var result = await CreateController().DeleteAsync(1);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Delete_ReturnsProblem_OnNotFound()
    {
        _svc.DeleteResult = Result.Failure(FaqErrors.NotFound);

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    private class FakeFaqService : IFaqService
    {
        public Result<List<FaqResponse>> GetAllResult { get; set; } = Result<List<FaqResponse>>.Success([]);
        public Result<FaqResponse> GetByIdResult { get; set; } =
            Result<FaqResponse>.Success(new(1, new(), new(), DateTime.UtcNow));
        public Result AddResult { get; set; } = Result.Success();
        public Result UpdateResult { get; set; } = Result.Success();
        public Result DeleteResult { get; set; } = Result.Success();

        public Task<Result<List<FaqResponse>>> GetAllAsync(FaqFilterRequest filter, CancellationToken ct = default) => Task.FromResult(GetAllResult);
        public Task<Result<FaqResponse>> GetByIdAsync(long id, CancellationToken ct = default) => Task.FromResult(GetByIdResult);
        public Task<Result> AddAsync(CreateFaqRequest r, CancellationToken ct = default) => Task.FromResult(AddResult);
        public Task<Result> UpdateAsync(long id, CreateFaqRequest r, CancellationToken ct = default) => Task.FromResult(UpdateResult);
        public Task<Result> DeleteAsync(long id, CancellationToken ct = default) => Task.FromResult(DeleteResult);
    }
}
