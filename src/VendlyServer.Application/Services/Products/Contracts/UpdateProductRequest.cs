using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Products.Contracts;

public record UpdateProductRequest(
    long CategoryId,
    MultiLanguageField Name,
    string? Description,
    bool IsActive,
    SyncSource SyncSource);
