using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Faqs;
using VendlyServer.Application.Services.Faqs.Contracts;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Tests.Services;

public class FaqServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly FaqService _service;

    public FaqServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new FaqService(_db);
    }

    private static MultiLanguageField Ml(string value) => new()
    {
        Uz = value, Ru = value, En = value, Cyrl = value
    };

    private async Task<Faq> SeedFaq(string question, bool isDeleted = false)
    {
        var faq = new Faq
        {
            Question = Ml(question),
            Answer = Ml("answer-" + question),
            IsDeleted = isDeleted
        };
        _db.Faqs.Add(faq);
        await _db.SaveChangesAsync();
        return faq;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsNonDeleted()
    {
        await SeedFaq("Shipping");
        await SeedFaq("Returns");
        await SeedFaq("Old", isDeleted: true);

        var result = await _service.GetAllAsync(new FaqFilterRequest(null));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task GetAll_FiltersBySearch()
    {
        await SeedFaq("Shipping");
        await SeedFaq("Returns");

        var result = await _service.GetAllAsync(new FaqFilterRequest("Ship"));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task GetAll_ReturnsEmpty_WhenNone()
    {
        var result = await _service.GetAllAsync(new FaqFilterRequest(null));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ReturnsFaq_WhenExists()
    {
        var faq = await SeedFaq("Shipping");

        var result = await _service.GetByIdAsync(faq.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(faq.Id, result.Data!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(FaqErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenDeleted()
    {
        var faq = await SeedFaq("Old", isDeleted: true);

        var result = await _service.GetByIdAsync(faq.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(FaqErrors.NotFound, result.Error);
    }

    // ── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_CreatesFaq()
    {
        var result = await _service.AddAsync(new CreateFaqRequest(Ml("Q1"), Ml("A1")));

        Assert.True(result.IsSuccess);
        Assert.Single(_db.Faqs);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ChangesFields_WhenExists()
    {
        var faq = await SeedFaq("Old");

        var result = await _service.UpdateAsync(faq.Id, new CreateFaqRequest(Ml("New"), Ml("NewAns")));

        Assert.True(result.IsSuccess);
        var updated = _db.Faqs.Single(f => f.Id == faq.Id);
        Assert.Equal("New", updated.Question.Uz);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.UpdateAsync(999, new CreateFaqRequest(Ml("X"), Ml("Y")));

        Assert.False(result.IsSuccess);
        Assert.Equal(FaqErrors.NotFound, result.Error);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletes_WhenExists()
    {
        var faq = await SeedFaq("Shipping");

        var result = await _service.DeleteAsync(faq.Id);

        Assert.True(result.IsSuccess);
        Assert.True(_db.Faqs.Single(f => f.Id == faq.Id).IsDeleted);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMissing()
    {
        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(FaqErrors.NotFound, result.Error);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
