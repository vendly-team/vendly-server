namespace VendlyServer.Application.Services.Addresses.Contracts;

public record AddressResponse(
    long Id,
    string Label,
    string City,
    string District,
    string Street,
    string House,
    string? Extra,
    string BtsCityCode,
    string? BtsBranchCode,
    bool IsDefault,
    DateTime CreatedAt
);
