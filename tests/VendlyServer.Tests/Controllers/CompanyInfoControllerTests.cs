using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Application.Services.CompanyInfo;
using VendlyServer.Application.Services.CompanyInfo.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Common;

namespace VendlyServer.Tests.Controllers;

public class CompanyInfoControllerTests
{
    private readonly FakeCompanyInfoService _svc = new();

    private CompanyInfoController CreateController()
    {
        var ctrl = new CompanyInfoController(_svc);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    private static CompanyInfoResponse Sample(string? name) => new(
        name, "phone", "email", "addr", "hours", "inn", "mfo", "bank", "acc",
        "tg", "ig", "fb", "yt", "brand", "logo.png", new MultiLanguageField(), DateTime.UtcNow);

    private static UpsertCompanyInfoRequest Request() => new(
        Name: "Vendly", Phone: null, Email: null, Address: null, WorkingHours: null,
        Inn: null, Mfo: null, BankName: null, AccountNumber: null,
        Telegram: null, Instagram: null, Facebook: null, YouTube: null,
        BrandName: null, Logo: null, OfertaUz: null, OfertaRu: null, OfertaEn: null, OfertaCyrl: null);

    [Fact]
    public async Task Get_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetResult = Result<CompanyInfoResponse>.Success(Sample("Vendly"));

        var result = await CreateController().GetAsync();

        var ok = Assert.IsType<Ok<CompanyInfoResponse>>(result);
        Assert.Equal("Vendly", ok.Value!.Name);
    }

    [Fact]
    public async Task Get_ReturnsProblem_OnFailure()
    {
        _svc.GetResult = Result<CompanyInfoResponse>.Failure(Error.Failure("CompanyInfo.Fail"));

        var result = await CreateController().GetAsync();

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Upsert_ReturnsOkWithData_OnSuccess()
    {
        _svc.UpsertResult = Result<CompanyInfoResponse>.Success(Sample("Updated"));

        var result = await CreateController().UpsertAsync(Request());

        var ok = Assert.IsType<Ok<CompanyInfoResponse>>(result);
        Assert.Equal("Updated", ok.Value!.Name);
    }

    [Fact]
    public async Task Upsert_ReturnsProblem_OnFailure()
    {
        _svc.UpsertResult = Result<CompanyInfoResponse>.Failure(Error.Failure("Storage.UploadFailed"));

        var result = await CreateController().UpsertAsync(Request());

        Assert.IsType<ProblemHttpResult>(result);
    }

    private class FakeCompanyInfoService : ICompanyInfoService
    {
        public Result<CompanyInfoResponse> GetResult { get; set; } =
            Result<CompanyInfoResponse>.Success(new(
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                new MultiLanguageField(), null));
        public Result<CompanyInfoResponse> UpsertResult { get; set; } =
            Result<CompanyInfoResponse>.Success(new(
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                new MultiLanguageField(), null));

        public Task<Result<CompanyInfoResponse>> GetAsync(CancellationToken ct = default) => Task.FromResult(GetResult);
        public Task<Result<CompanyInfoResponse>> UpsertAsync(UpsertCompanyInfoRequest r, CancellationToken ct = default) => Task.FromResult(UpsertResult);
    }
}
