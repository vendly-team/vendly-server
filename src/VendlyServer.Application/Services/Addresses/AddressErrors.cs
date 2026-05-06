using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Addresses;

public static class AddressErrors
{
    public static readonly Error NotFound = Error.NotFound("Address.NotFound");

    public static readonly Error LimitReached = Error.Conflict("Address.LimitReached");

    public static readonly Error BtsCityCodeInvalid =
        Error.Validation("Address.BtsCityCodeInvalid", "BtsCityCode does not exist.");
}
