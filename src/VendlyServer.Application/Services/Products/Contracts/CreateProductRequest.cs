using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Products.Contracts;

public record CreateProductRequest(
    long CategoryId,
    string Name,
    string? Description,
    SyncSource SyncSource = SyncSource.Manual);
