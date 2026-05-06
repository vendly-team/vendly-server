namespace VendlyServer.Application.Services.Addresses.Contracts;

public record UpdateAddressRequest(
    string Label,
    string City,
    string District,
    string Street,
    string House,
    string? Extra,
    string BtsCityCode,
    bool IsDefault
);
