using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Orders;
using VendlyServer.Application.Services.ReturnReasons;
using VendlyServer.Application.Services.ReturnReasons.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Tests.Controllers;

public class ReturnReasonsControllerTests
{
    private readonly FakeReturnReasonService _svc = new();

    private ReturnReasonsController CreateController()
    {
        var ctrl = new ReturnReasonsController(_svc);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    private static MultiLanguageField Ml(string v) => new() { Uz = v, Ru = v, En = v, Cyrl = v };

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetAllResult = Result<List<ReturnReasonResponse>>.Success(
        [
            new(1, "damaged", Ml("Damaged"), Ml("d"), true, DateTime.UtcNow),
            new(2, "wrong", Ml("Wrong"), Ml("w"), false, DateTime.UtcNow)
        ]);

        var result = await CreateController().GetAllAsync(new ReturnReasonFilterRequest(null));

        var ok = Assert.IsType<Ok<List<ReturnReasonResponse>>>(result);
        Assert.Equal(2, ok.Value!.Count);
    }

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetByIdResult = Result<ReturnReasonResponse>.Success(
            new(1, "damaged", Ml("Damaged"), Ml("d"), true, DateTime.UtcNow));

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<ReturnReasonResponse>>(result);
        Assert.Equal("damaged", ok.Value!.Key);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetByIdResult = Result<ReturnReasonResponse>.Failure(ReturnReasonErrors.NotFound);

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsOk_OnSuccess()
    {
        _svc.AddResult = Result.Success();

        var result = await CreateController().AddAsync(
            new CreateReturnReasonRequest("damaged", Ml("D"), Ml("d"), true));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnKeyExists()
    {
        _svc.AddResult = Result.Failure(ReturnReasonErrors.KeyExists);

        var result = await CreateController().AddAsync(
            new CreateReturnReasonRequest("damaged", Ml("D"), Ml("d"), true));

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        _svc.UpdateResult = Result.Success();

        var result = await CreateController().UpdateAsync(1,
            new CreateReturnReasonRequest("damaged", Ml("D"), Ml("d"), true));

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_OnNotFound()
    {
        _svc.UpdateResult = Result.Failure(ReturnReasonErrors.NotFound);

        var result = await CreateController().UpdateAsync(999,
            new CreateReturnReasonRequest("damaged", Ml("D"), Ml("d"), true));

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
        _svc.DeleteResult = Result.Failure(ReturnReasonErrors.NotFound);

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    private class FakeReturnReasonService : IReturnReasonService
    {
        public Result<List<ReturnReasonResponse>> GetAllResult { get; set; } = Result<List<ReturnReasonResponse>>.Success([]);
        public Result<ReturnReasonResponse> GetByIdResult { get; set; } =
            Result<ReturnReasonResponse>.Success(new(1, "k", new(), new(), false, DateTime.UtcNow));
        public Result AddResult { get; set; } = Result.Success();
        public Result UpdateResult { get; set; } = Result.Success();
        public Result DeleteResult { get; set; } = Result.Success();

        public Task<Result<List<ReturnReasonResponse>>> GetAllAsync(ReturnReasonFilterRequest filter, CancellationToken ct = default) => Task.FromResult(GetAllResult);
        public Task<Result<ReturnReasonResponse>> GetByIdAsync(long id, CancellationToken ct = default) => Task.FromResult(GetByIdResult);
        public Task<Result> AddAsync(CreateReturnReasonRequest r, CancellationToken ct = default) => Task.FromResult(AddResult);
        public Task<Result> UpdateAsync(long id, CreateReturnReasonRequest r, CancellationToken ct = default) => Task.FromResult(UpdateResult);
        public Task<Result> DeleteAsync(long id, CancellationToken ct = default) => Task.FromResult(DeleteResult);
    }
}
