using VendlyServer.Application.Services.CategoryPrices.Contracts;
using VendlyServer.Domain.Entities.Catalogs;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.CategoryPrices;

public class CategoryPriceService(AppDbContext dbContext) : ICategoryPriceService
{
    public async Task<Result<List<CategoryPriceResponse>>> GetAllAsync(
        long? categoryId, CancellationToken cancellationToken = default)
    {
        var prices = await dbContext.CategoryPrices
            .AsNoTracking()
            .Where(cp => !cp.IsDeleted)
            .Where(cp => categoryId == null || cp.CategoryId == categoryId)
            .OrderByDescending(cp => cp.CreatedAt)
            .Select(cp => new CategoryPriceResponse(
                cp.Id,
                cp.CategoryId,
                cp.MarkupType,
                cp.Value,
                cp.RoundingStep,
                cp.StartDate,
                cp.EndDate,
                cp.CreatedAt,
                cp.UpdatedAt))
            .ToListAsync(cancellationToken);

        return prices;
    }

    public async Task<Result<CategoryPriceResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var price = await dbContext.CategoryPrices
            .AsNoTracking()
            .Where(cp => cp.Id == id && !cp.IsDeleted)
            .Select(cp => new CategoryPriceResponse(
                cp.Id,
                cp.CategoryId,
                cp.MarkupType,
                cp.Value,
                cp.RoundingStep,
                cp.StartDate,
                cp.EndDate,
                cp.CreatedAt,
                cp.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return price is null ? CategoryPriceErrors.NotFound : price;
    }

    public async Task<Result<long>> AddAsync(CreateCategoryPriceRequest request, CancellationToken cancellationToken = default)
    {
        var categoryExists = await dbContext.Categories
            .AnyAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);

        if (!categoryExists) return CategoryPriceErrors.CategoryNotFound;

        var price = new CategoryPrice
        {
            CategoryId = request.CategoryId,
            MarkupType = request.MarkupType,
            Value = request.Value,
            RoundingStep = request.RoundingStep,
            StartDate = ToUtc(request.StartDate),
            EndDate = ToUtc(request.EndDate)
        };

        dbContext.CategoryPrices.Add(price);
        await dbContext.SaveChangesAsync(cancellationToken);
        return price.Id;
    }

    public async Task<Result> UpdateAsync(long id, UpdateCategoryPriceRequest request, CancellationToken cancellationToken = default)
    {
        var price = await dbContext.CategoryPrices
            .SingleOrDefaultAsync(cp => cp.Id == id && !cp.IsDeleted, cancellationToken);

        if (price is null) return CategoryPriceErrors.NotFound;

        var categoryExists = await dbContext.Categories
            .AnyAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);

        if (!categoryExists) return CategoryPriceErrors.CategoryNotFound;

        price.CategoryId = request.CategoryId;
        price.MarkupType = request.MarkupType;
        price.Value = request.Value;
        price.RoundingStep = request.RoundingStep;
        price.StartDate = ToUtc(request.StartDate);
        price.EndDate = ToUtc(request.EndDate);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // Npgsql 'timestamp with time zone' faqat UTC qabul qiladi. Frontend offsetsiz
    // (Kind=Unspecified) sana yuboradi — uni UTC sifatida belgilaymiz.
    private static DateTime? ToUtc(DateTime? value) => value?.Kind switch
    {
        null => null,
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.Value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
    };

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var price = await dbContext.CategoryPrices
            .SingleOrDefaultAsync(cp => cp.Id == id && !cp.IsDeleted, cancellationToken);

        if (price is null) return CategoryPriceErrors.NotFound;

        price.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
