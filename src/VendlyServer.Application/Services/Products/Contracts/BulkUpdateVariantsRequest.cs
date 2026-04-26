namespace VendlyServer.Application.Services.Products.Contracts;

public record BulkUpdateVariantsRequest(List<BulkUpdateVariantItem> Variants);
