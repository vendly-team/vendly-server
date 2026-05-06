using VendlyServer.Application.Services.Addresses.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Addresses;

public interface IAddressService
{
    Task<Result<List<AddressResponse>>> GetAllForUserAsync(long userId, CancellationToken cancellationToken = default);

    Task<Result<AddressResponse>> GetByIdAsync(long userId, long id, CancellationToken cancellationToken = default);

    Task<Result<AddressResponse>> AddAsync(long userId, CreateAddressRequest request, CancellationToken cancellationToken = default);

    Task<Result<AddressResponse>> UpdateAsync(long userId, long id, UpdateAddressRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(long userId, long id, CancellationToken cancellationToken = default);

    Task<Result<AddressResponse>> SetDefaultAsync(long userId, long id, CancellationToken cancellationToken = default);
}
