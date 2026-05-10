using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VendlyServer.Api.Controllers.Public;
using VendlyServer.Application.Services.Addresses;
using VendlyServer.Application.Services.Addresses.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Tests.Controllers;

public class AddressesControllerTests
{
    private readonly FakeAddressService _svc = new();

    private AddressesController CreateController(long userId = 1)
    {
        var ctrl = new AddressesController(_svc);
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

    private static AddressResponse Sample(long id = 1, bool isDefault = true) =>
        new(id, "Home", "Toshkent shahri", "Uchtepa", "Bobur", "12", null, "0101", isDefault, DateTime.UtcNow);

    private static CreateAddressRequest SampleCreate() =>
        new("Home", "Toshkent shahri", "Uchtepa", "Bobur", "12", null, "0101", true);

    private static UpdateAddressRequest SampleUpdate() =>
        new("Home", "Toshkent shahri", "Uchtepa", "Bobur", "12", null, "0101", true);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetAllResult = new List<AddressResponse> { Sample(1), Sample(2, false) };

        var result = await CreateController().GetAllAsync();

        var ok = Assert.IsType<Ok<List<AddressResponse>>>(result);
        Assert.Equal(2, ok.Value!.Count);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsOkWithData_OnSuccess()
    {
        _svc.GetByIdResult = Sample();

        var result = await CreateController().GetByIdAsync(1);

        var ok = Assert.IsType<Ok<AddressResponse>>(result);
        Assert.Equal(1, ok.Value!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsProblem_OnNotFound()
    {
        _svc.GetByIdError = AddressErrors.NotFound;

        var result = await CreateController().GetByIdAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_ReturnsOkWithData_OnSuccess()
    {
        _svc.AddResult = Sample();

        var result = await CreateController().AddAsync(SampleCreate());

        var ok = Assert.IsType<Ok<AddressResponse>>(result);
        Assert.Equal("Home", ok.Value!.Label);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnLimitReached()
    {
        _svc.AddError = AddressErrors.LimitReached;

        var result = await CreateController().AddAsync(SampleCreate());

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsProblem_OnInvalidBtsCity()
    {
        _svc.AddError = AddressErrors.BtsCityCodeInvalid;

        var result = await CreateController().AddAsync(SampleCreate());

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        _svc.UpdateResult = Sample();

        var result = await CreateController().UpdateAsync(1, SampleUpdate());

        Assert.IsType<Ok<AddressResponse>>(result);
    }

    [Fact]
    public async Task Update_ReturnsProblem_OnNotFound()
    {
        _svc.UpdateError = AddressErrors.NotFound;

        var result = await CreateController().UpdateAsync(999, SampleUpdate());

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
        _svc.DeleteError = AddressErrors.NotFound;

        var result = await CreateController().DeleteAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── SetDefault ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetDefault_ReturnsOkWithData_OnSuccess()
    {
        _svc.SetDefaultResult = Sample();

        var result = await CreateController().SetDefaultAsync(1);

        Assert.IsType<Ok<AddressResponse>>(result);
    }

    [Fact]
    public async Task SetDefault_ReturnsProblem_OnNotFound()
    {
        _svc.SetDefaultError = AddressErrors.NotFound;

        var result = await CreateController().SetDefaultAsync(999);

        Assert.IsType<ProblemHttpResult>(result);
    }

    // ── Fake service ──────────────────────────────────────────────────────────

    private class FakeAddressService : IAddressService
    {
        public List<AddressResponse>? GetAllResult;
        public AddressResponse? GetByIdResult;
        public Error? GetByIdError;
        public AddressResponse? AddResult;
        public Error? AddError;
        public AddressResponse? UpdateResult;
        public Error? UpdateError;
        public bool DeleteSuccess;
        public Error? DeleteError;
        public AddressResponse? SetDefaultResult;
        public Error? SetDefaultError;

        public Task<Result<List<AddressResponse>>> GetAllForUserAsync(long userId, CancellationToken ct = default)
            => Task.FromResult<Result<List<AddressResponse>>>(GetAllResult ?? []);

        public Task<Result<AddressResponse>> GetByIdAsync(long userId, long id, CancellationToken ct = default)
            => Task.FromResult(GetByIdError is { } e
                ? Result<AddressResponse>.Failure(e)
                : Result<AddressResponse>.Success(GetByIdResult!));

        public Task<Result<AddressResponse>> AddAsync(long userId, CreateAddressRequest request, CancellationToken ct = default)
            => Task.FromResult(AddError is { } e
                ? Result<AddressResponse>.Failure(e)
                : Result<AddressResponse>.Success(AddResult!));

        public Task<Result<AddressResponse>> UpdateAsync(long userId, long id, UpdateAddressRequest request, CancellationToken ct = default)
            => Task.FromResult(UpdateError is { } e
                ? Result<AddressResponse>.Failure(e)
                : Result<AddressResponse>.Success(UpdateResult!));

        public Task<Result> DeleteAsync(long userId, long id, CancellationToken ct = default)
            => Task.FromResult(DeleteError is { } e ? Result.Failure(e) : Result.Success());

        public Task<Result<AddressResponse>> SetDefaultAsync(long userId, long id, CancellationToken ct = default)
            => Task.FromResult(SetDefaultError is { } e
                ? Result<AddressResponse>.Failure(e)
                : Result<AddressResponse>.Success(SetDefaultResult!));
    }
}
