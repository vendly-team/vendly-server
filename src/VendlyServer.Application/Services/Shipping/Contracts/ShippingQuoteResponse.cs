namespace VendlyServer.Application.Services.Shipping.Contracts;

public record ShippingQuoteResponse(decimal Cost, string DropoffType, string Currency);
