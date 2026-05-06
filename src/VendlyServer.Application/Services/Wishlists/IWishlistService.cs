using VendlyServer.Application.Services.Wishlists.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Wishlists;

public interface IWishlistService
{
    Task<Result<List<WishlistResponse>>> GetAllAsync(long userId, CancellationToken cancellationToken = default);
    Task<Result<WishlistResponse>> GetByIdAsync(long id, long userId, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(long userId, AddWishlistRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(long id, long userId, CancellationToken cancellationToken = default);
}
