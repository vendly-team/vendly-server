using VendlyServer.Domain.Enums;

namespace VendlyServer.Application.Services.Products.Contracts;

public record UpdateProductRequest(
    long CategoryId,
    string Name,
    string? Description,
    bool IsActive,
    SyncSource SyncSource);
