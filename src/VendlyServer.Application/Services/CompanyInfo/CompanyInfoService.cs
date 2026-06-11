using Microsoft.Extensions.Logging;
using VendlyServer.Application.Services.CompanyInfo.Contracts;
using VendlyServer.Application.Services.Storages;
using VendlyServer.Domain.Entities.Common;
using VendlyServer.Infrastructure.Persistence;
using CompanyInfoEntity = VendlyServer.Domain.Entities.Public.CompanyInfo;

namespace VendlyServer.Application.Services.CompanyInfo;

public class CompanyInfoService(
    AppDbContext dbContext,
    IStorageService storageService,
    ILogger<CompanyInfoService> logger) : ICompanyInfoService
{
    public async Task<Result<CompanyInfoResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var info = await dbContext.CompanyInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return info is null ? EmptyResponse() : MapToResponse(info);
    }

    public async Task<Result<CompanyInfoResponse>> UpsertAsync(
        UpsertCompanyInfoRequest request, CancellationToken cancellationToken = default)
    {
        var info = await dbContext.CompanyInfos
            .FirstOrDefaultAsync(cancellationToken);

        var isNew = info is null;
        info ??= new CompanyInfoEntity();

        // Scalar maydonlar
        info.Name = request.Name;
        info.Phone = request.Phone;
        info.Email = request.Email;
        info.Address = request.Address;
        info.WorkingHours = request.WorkingHours;
        info.Inn = request.Inn;
        info.Mfo = request.Mfo;
        info.BankName = request.BankName;
        info.AccountNumber = request.AccountNumber;
        info.Telegram = request.Telegram;
        info.Instagram = request.Instagram;
        info.Facebook = request.Facebook;
        info.YouTube = request.YouTube;
        info.BrandName = request.BrandName;

        // Logo (rasm)
        var logo = await ReplaceFileAsync(request.Logo, info.LogoUrl, "company", cancellationToken);
        if (!logo.IsSuccess) return logo.Error;
        info.LogoUrl = logo.Data;

        // Oferta PDF — har til
        var uz = await ReplaceFileAsync(request.OfertaUz, info.OfertaUrl.Uz, "oferta", cancellationToken);
        if (!uz.IsSuccess) return uz.Error;
        info.OfertaUrl.Uz = uz.Data;

        var ru = await ReplaceFileAsync(request.OfertaRu, info.OfertaUrl.Ru, "oferta", cancellationToken);
        if (!ru.IsSuccess) return ru.Error;
        info.OfertaUrl.Ru = ru.Data;

        var en = await ReplaceFileAsync(request.OfertaEn, info.OfertaUrl.En, "oferta", cancellationToken);
        if (!en.IsSuccess) return en.Error;
        info.OfertaUrl.En = en.Data;

        var cyrl = await ReplaceFileAsync(request.OfertaCyrl, info.OfertaUrl.Cyrl, "oferta", cancellationToken);
        if (!cyrl.IsSuccess) return cyrl.Error;
        info.OfertaUrl.Cyrl = cyrl.Data;

        if (isNew)
            dbContext.CompanyInfos.Add(info);

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapToResponse(info);
    }

    // Fayl berilgan bo'lsa yuklaydi (eskisini o'chiradi), aks holda eski URL saqlanadi.
    private async Task<Result<string?>> ReplaceFileAsync(
        Microsoft.AspNetCore.Http.IFormFile? file, string? oldUrl, string folder, CancellationToken cancellationToken)
    {
        if (file is null) return Result<string?>.Success(oldUrl);

        var upload = await storageService.UploadAsync(file, folder, cancellationToken);
        if (!upload.IsSuccess) return upload.Error;

        if (oldUrl is not null)
            await TryDeleteFileAsync(oldUrl);

        return Result<string?>.Success(upload.Data);
    }

    private static CompanyInfoResponse MapToResponse(CompanyInfoEntity info) => new(
        info.Name,
        info.Phone,
        info.Email,
        info.Address,
        info.WorkingHours,
        info.Inn,
        info.Mfo,
        info.BankName,
        info.AccountNumber,
        info.Telegram,
        info.Instagram,
        info.Facebook,
        info.YouTube,
        info.BrandName,
        info.LogoUrl,
        info.OfertaUrl,
        info.UpdatedAt);

    private static CompanyInfoResponse EmptyResponse() => new(
        null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
        new MultiLanguageField(), null);

    private async Task TryDeleteFileAsync(string fileUrl)
    {
        var result = await storageService.DeleteAsync(fileUrl);
        if (!result.IsSuccess)
            logger.LogWarning("Failed to delete company file: {Url}", fileUrl);
    }
}
