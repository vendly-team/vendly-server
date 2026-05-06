using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Addresses;
using VendlyServer.Application.Services.Addresses.Contracts;
using VendlyServer.Domain.Entities.Ref;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class AddressServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AddressService _service;
    private const long UserA = 1;
    private const long UserB = 2;
    private const string ValidCityCode = "0101";
    private const string OtherCityCode = "0102";

    public AddressServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new AddressService(_db);

        _db.BtsCities.AddRange(
            new BtsCityRef { RegionCode = "01", Code = ValidCityCode, Name = "Uchtepa", SyncedAt = DateTime.UtcNow },
            new BtsCityRef { RegionCode = "01", Code = OtherCityCode, Name = "Yunusobod", SyncedAt = DateTime.UtcNow });
        _db.SaveChanges();
    }

    private static CreateAddressRequest NewRequest(string label = "Home", bool isDefault = false, string btsCityCode = ValidCityCode) =>
        new(
            Label: label,
            City: "Toshkent shahri",
            District: "Uchtepa",
            Street: "Bobur",
            House: "12",
            Extra: null,
            BtsCityCode: btsCityCode,
            IsDefault: isDefault);

    private Address Seed(long userId, string label = "Old", bool isDefault = false, bool isDeleted = false, string btsCityCode = ValidCityCode)
    {
        var entity = new Address
        {
            UserId = userId,
            Label = label,
            City = "Toshkent shahri",
            District = "Uchtepa",
            Street = "Bobur",
            House = "12",
            BtsCityCode = btsCityCode,
            IsDefault = isDefault,
            IsDeleted = isDeleted
        };
        _db.Addresses.Add(entity);
        _db.SaveChanges();
        return entity;
    }

    // ── AddAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_FirstAddress_AutomaticallyBecomesDefault()
    {
        var result = await _service.AddAsync(UserA, NewRequest(isDefault: false));

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsDefault);
    }

    [Fact]
    public async Task Add_NonFirst_RespectsIsDefaultFalse()
    {
        Seed(UserA, isDefault: true);

        var result = await _service.AddAsync(UserA, NewRequest(label: "Work", isDefault: false));

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.IsDefault);
    }

    [Fact]
    public async Task Add_NonFirst_WhenIsDefaultTrue_FlipsOldDefault()
    {
        var existing = Seed(UserA, label: "Old", isDefault: true);

        var result = await _service.AddAsync(UserA, NewRequest(label: "New", isDefault: true));

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsDefault);

        var oldRow = await _db.Addresses.FindAsync(existing.Id);
        Assert.False(oldRow!.IsDefault);
    }

    [Fact]
    public async Task Add_ReturnsBtsCityCodeInvalid_WhenCityDoesNotExist()
    {
        var result = await _service.AddAsync(UserA, NewRequest(btsCityCode: "9999"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AddressErrors.BtsCityCodeInvalid, result.Error);
    }

    [Fact]
    public async Task Add_ReturnsLimitReached_When10Already()
    {
        for (int i = 0; i < 10; i++)
            Seed(UserA, label: $"Addr-{i}");

        var result = await _service.AddAsync(UserA, NewRequest(label: "Eleventh"));

        Assert.False(result.IsSuccess);
        Assert.Equal(AddressErrors.LimitReached, result.Error);
    }

    // ── GetAllForUserAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyOwnNonDeletedAddresses()
    {
        Seed(UserA, label: "MineA");
        Seed(UserA, label: "MineB");
        Seed(UserA, label: "MineDeleted", isDeleted: true);
        Seed(UserB, label: "Stranger");

        var result = await _service.GetAllForUserAsync(UserA);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
        Assert.All(result.Data!, a => Assert.Contains(a.Label, new[] { "MineA", "MineB" }));
    }

    [Fact]
    public async Task GetAll_OrdersDefaultFirst()
    {
        Seed(UserA, label: "Plain", isDefault: false);
        Seed(UserA, label: "Default", isDefault: true);

        var result = await _service.GetAllForUserAsync(UserA);

        Assert.True(result.IsSuccess);
        Assert.Equal("Default", result.Data!.First().Label);
    }

    // ── GetByIdAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenOtherUsersAddress()
    {
        var stranger = Seed(UserB, label: "Stranger");

        var result = await _service.GetByIdAsync(UserA, stranger.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(AddressErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_ReturnsAddress_WhenOwn()
    {
        var own = Seed(UserA, label: "Mine");

        var result = await _service.GetByIdAsync(UserA, own.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Mine", result.Data!.Label);
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ChangesFields_WhenOwn()
    {
        var own = Seed(UserA, label: "Old");

        var result = await _service.UpdateAsync(UserA, own.Id, new UpdateAddressRequest(
            Label: "New",
            City: "Toshkent shahri",
            District: "Yunusobod",
            Street: "Amir Temur",
            House: "55",
            Extra: "Apartment 4",
            BtsCityCode: OtherCityCode,
            IsDefault: false));

        Assert.True(result.IsSuccess);
        Assert.Equal("New", result.Data!.Label);
        Assert.Equal(OtherCityCode, result.Data.BtsCityCode);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenOtherUsersAddress()
    {
        var stranger = Seed(UserB, label: "Stranger");

        var result = await _service.UpdateAsync(UserA, stranger.Id, new UpdateAddressRequest(
            Label: "Hijack",
            City: "x", District: "x", Street: "x", House: "1", Extra: null,
            BtsCityCode: ValidCityCode, IsDefault: false));

        Assert.False(result.IsSuccess);
        Assert.Equal(AddressErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Update_ReturnsBtsCityCodeInvalid_WhenChangedToUnknownCode()
    {
        var own = Seed(UserA);

        var result = await _service.UpdateAsync(UserA, own.Id, new UpdateAddressRequest(
            Label: "X", City: "X", District: "X", Street: "X", House: "1", Extra: null,
            BtsCityCode: "9999", IsDefault: false));

        Assert.False(result.IsSuccess);
        Assert.Equal(AddressErrors.BtsCityCodeInvalid, result.Error);
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletes_WhenOwn()
    {
        var own = Seed(UserA);

        var result = await _service.DeleteAsync(UserA, own.Id);

        Assert.True(result.IsSuccess);
        var row = await _db.Addresses.FindAsync(own.Id);
        Assert.True(row!.IsDeleted);
    }

    [Fact]
    public async Task Delete_PromotesAnotherToDefault_WhenDeletedWasDefault()
    {
        var defaultAddr = Seed(UserA, label: "Default", isDefault: true);
        var other = Seed(UserA, label: "Other", isDefault: false);

        var result = await _service.DeleteAsync(UserA, defaultAddr.Id);

        Assert.True(result.IsSuccess);
        var promoted = await _db.Addresses.FindAsync(other.Id);
        Assert.True(promoted!.IsDefault);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_ForOtherUser()
    {
        var stranger = Seed(UserB);

        var result = await _service.DeleteAsync(UserA, stranger.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(AddressErrors.NotFound, result.Error);
    }

    // ── SetDefaultAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetDefault_FlipsOldDefault()
    {
        var oldDefault = Seed(UserA, label: "Old", isDefault: true);
        var newDefault = Seed(UserA, label: "New", isDefault: false);

        var result = await _service.SetDefaultAsync(UserA, newDefault.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsDefault);

        var oldRow = await _db.Addresses.FindAsync(oldDefault.Id);
        Assert.False(oldRow!.IsDefault);
    }

    [Fact]
    public async Task SetDefault_ReturnsNotFound_WhenOtherUsersAddress()
    {
        var stranger = Seed(UserB);

        var result = await _service.SetDefaultAsync(UserA, stranger.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(AddressErrors.NotFound, result.Error);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
