using VendlyServer.Domain.Entities.Common;
using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Products.Contracts;

public record CreateProductRequest(
    long CategoryId,
    MultiLanguageField Name,
    string? Description,
    SyncSource SyncSource = SyncSource.Manual);
