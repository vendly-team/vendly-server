using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Faqs.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Faqs;

public class FaqService(AppDbContext dbContext) : IFaqService
{
    public async Task<Result<List<FaqResponse>>> GetAllAsync(FaqFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var faqs = await dbContext.Faqs
            .AsNoTracking()
            .Where(f => !f.IsDeleted)
            .Where(f => filter.Search == null || f.Question.Uz!.Contains(filter.Search) || f.Question.Ru!.Contains(filter.Search))
            .ToListAsync(cancellationToken);

        return faqs
            .Select(f => new FaqResponse(f.Id, f.Question, f.Answer, f.CreatedAt))
            .ToList();
    }

    public async Task<Result<FaqResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var faq = await dbContext.Faqs
            .AsNoTracking()
            .Where(f => f.Id == id && !f.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        return faq is null
            ? FaqErrors.NotFound
            : new FaqResponse(faq.Id, faq.Question, faq.Answer, faq.CreatedAt);
    }

    public async Task<Result> AddAsync(CreateFaqRequest request, CancellationToken cancellationToken = default)
    {
        dbContext.Faqs.Add(new Faq
        {
            Question = request.Question,
            Answer   = request.Answer,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(long id, CreateFaqRequest request, CancellationToken cancellationToken = default)
    {
        var faq = await dbContext.Faqs
            .SingleOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);

        if (faq is null) return FaqErrors.NotFound;

        faq.Question = request.Question;
        faq.Answer   = request.Answer;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var faq = await dbContext.Faqs
            .SingleOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);

        if (faq is null) return FaqErrors.NotFound;

        faq.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
