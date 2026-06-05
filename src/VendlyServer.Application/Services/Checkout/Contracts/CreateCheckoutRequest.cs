namespace VendlyServer.Application.Services.Checkout.Contracts;

/// <summary>Starts a checkout for the current user's cart, delivered to the given saved address.</summary>
public record CreateCheckoutRequest(long AddressId);
