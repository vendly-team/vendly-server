using VendlyServer.Application.Services.Carts.Contracts;
using VendlyServer.Domain.Abstractions;

namespace VendlyServer.Application.Services.Carts;

public interface ICartService
{
    Task<Result<CartResponse>> GetOrCreateAsync(long userId, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> AddItemAsync(long userId, CartItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> UpdateItemAsync(long userId, long cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<CartResponse>> RemoveItemAsync(long userId, long cartItemId, CancellationToken cancellationToken = default);
    Task<Result> ClearAsync(long userId, CancellationToken cancellationToken = default);
}
