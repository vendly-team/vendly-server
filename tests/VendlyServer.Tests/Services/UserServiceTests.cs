using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VendlyServer.Application.Services.Users;
using VendlyServer.Application.Services.Users.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Authentication;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly UserService _service;
    private readonly IMemoryCache _cache;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new UserService(_db, new FakePasswordHasher(), _cache);

        _db.Users.AddRange(
            new User { Id = 1, FirstName = "Alice",   LastName = "A", Phone = "1111111111", PasswordHash = "h1", Role = UserRole.Customer, IsBlocked = false },
            new User { Id = 2, FirstName = "Bob",     LastName = "B", Phone = "2222222222", PasswordHash = "h2", Role = UserRole.Admin,    IsBlocked = false },
            new User { Id = 3, FirstName = "Carol",   LastName = "C", Phone = "3333333333", PasswordHash = "h3", Role = UserRole.Manager,  IsBlocked = false },
            new User { Id = 4, FirstName = "Deleted", LastName = "D", Phone = "4444444444", PasswordHash = "h4", Role = UserRole.Customer, IsDeleted = true  }
        );
        _db.SaveChanges();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyNonDeletedUsers()
    {
        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data!.Count);
        Assert.DoesNotContain(result.Data, u => u.FirstName == "Deleted");
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsUser_WhenFound()
    {
        var result = await _service.GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alice", result.Data!.FirstName);
        Assert.Equal(UserRole.Customer, result.Data.Role);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenDeleted()
    {
        var result = await _service.GetByIdAsync(4);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_ReturnsEmptyOrdersAndReviews_WhenNoRelatedData()
    {
        var result = await _service.GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!.Orders);
        Assert.Empty(result.Data.Reviews);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_CreatesNewUser_WhenPhoneIsUnique()
    {
        var request = new CreateUserRequest("New", "User", "9999999999", "pass", null, UserRole.Customer);

        var result = await _service.CreateAsync(request);

        Assert.True(result.IsSuccess);
        var saved = await _db.Users.SingleOrDefaultAsync(u => u.Phone == "9999999999");
        Assert.NotNull(saved);
        Assert.Equal("pass_hashed", saved.PasswordHash);
    }

    [Fact]
    public async Task Create_RestoresUser_WhenPhoneExistsButDeleted()
    {
        var request = new CreateUserRequest("Restored", "User", "4444444444", "newpass", null, UserRole.Manager);

        var result = await _service.CreateAsync(request);

        Assert.True(result.IsSuccess);
        var restored = await _db.Users.FindAsync(4L);
        Assert.NotNull(restored);
        Assert.False(restored.IsDeleted);
        Assert.Equal("Restored", restored.FirstName);
        Assert.Equal(UserRole.Manager, restored.Role);
        Assert.Equal("newpass_hashed", restored.PasswordHash);
    }

    [Fact]
    public async Task Create_ReturnsAlreadyExists_WhenPhoneAlreadyActive()
    {
        var request = new CreateUserRequest("Dup", "User", "1111111111", "pass", null, UserRole.Customer);

        var result = await _service.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.AlreadyExists, result.Error);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_UpdatesFields_WhenUserFound()
    {
        var request = new UpdateUserRequest("AliceUpd", "AUpd", "1111111111", "alice@new.com");

        var result = await _service.UpdateAsync(1, request);

        Assert.True(result.IsSuccess);
        var updated = await _db.Users.FindAsync(1L);
        Assert.Equal("AliceUpd", updated!.FirstName);
        Assert.Equal("alice@new.com", updated.Email);
        Assert.Equal(UserRole.Customer, updated.Role); // role unchanged
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenUserMissing()
    {
        var result = await _service.UpdateAsync(999, new UpdateUserRequest("X", "Y", "0000000000", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Update_ReturnsAlreadyExists_WhenPhoneConflictsWithAnotherUser()
    {
        var request = new UpdateUserRequest("Alice", "A", "2222222222", null); // Bob's phone

        var result = await _service.UpdateAsync(1, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.AlreadyExists, result.Error);
    }

    // ── BlockAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Block_TogglesIsBlocked_WhenCallerIsAdmin()
    {
        var result = await _service.BlockAsync(1, UserRole.Admin);

        Assert.True(result.IsSuccess);
        var user = await _db.Users.FindAsync(1L);
        Assert.True(user!.IsBlocked);
    }

    [Fact]
    public async Task Block_TogglesIsBlocked_WhenManagerTargetsCustomer()
    {
        var result = await _service.BlockAsync(1, UserRole.Manager); // id=1 is Customer

        Assert.True(result.IsSuccess);
        var user = await _db.Users.FindAsync(1L);
        Assert.True(user!.IsBlocked);
    }

    [Fact]
    public async Task Block_ReturnsForbidden_WhenManagerTargetsAdmin()
    {
        var result = await _service.BlockAsync(2, UserRole.Manager); // id=2 is Admin

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.Forbidden, result.Error);
    }

    [Fact]
    public async Task Block_ReturnsForbidden_WhenManagerTargetsManager()
    {
        var result = await _service.BlockAsync(3, UserRole.Manager); // id=3 is Manager

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.Forbidden, result.Error);
    }

    [Fact]
    public async Task Block_ReturnsNotFound_WhenUserMissing()
    {
        var result = await _service.BlockAsync(999, UserRole.Admin);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Block_UnblocksUser_WhenAlreadyBlocked()
    {
        var user = await _db.Users.FindAsync(1L);
        user!.IsBlocked = true;
        await _db.SaveChangesAsync();

        var result = await _service.BlockAsync(1, UserRole.Admin);

        Assert.True(result.IsSuccess);
        var updated = await _db.Users.FindAsync(1L);
        Assert.False(updated!.IsBlocked);
    }

    // ── AssignRoleAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AssignRole_ChangesRole_WhenUserFound()
    {
        var result = await _service.AssignRoleAsync(1, new AssignRoleRequest(UserRole.Manager));

        Assert.True(result.IsSuccess);
        var user = await _db.Users.FindAsync(1L);
        Assert.Equal(UserRole.Manager, user!.Role);
    }

    [Fact]
    public async Task AssignRole_ReturnsNotFound_WhenUserMissing()
    {
        var result = await _service.AssignRoleAsync(999, new AssignRoleRequest(UserRole.Admin));

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.NotFound, result.Error);
    }

    // ── Cache invalidation ────────────────────────────────────────────────────

    [Fact]
    public async Task Block_InvalidatesCache_ForTargetUser()
    {
        var cacheKey = $"user_me:1";
        _cache.Set(cacheKey, "cached_value");

        await _service.BlockAsync(1, UserRole.Admin);

        Assert.False(_cache.TryGetValue(cacheKey, out _));
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        _cache.Dispose();
    }

    private class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"{password}_hashed";
        public bool Verify(string password, string hash) => hash == $"{password}_hashed";
    }
}
