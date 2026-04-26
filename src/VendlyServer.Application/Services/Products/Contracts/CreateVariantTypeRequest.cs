namespace VendlyServer.Application.Services.Products.Contracts;

public record CreateVariantTypeRequest(string Name, int? DisplayOrder = null);
