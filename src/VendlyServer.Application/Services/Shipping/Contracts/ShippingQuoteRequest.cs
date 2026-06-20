namespace VendlyServer.Application.Services.Shipping.Contracts;

public record ShippingQuoteRequest(string ReceiverCityCode, string? ReceiverBranchCode, double WeightKg);
