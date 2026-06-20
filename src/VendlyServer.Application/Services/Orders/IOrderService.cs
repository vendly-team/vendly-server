using VendlyServer.Domain.Abstractions;
using VendlyServer.Application.Services.Orders.Contracts;
using VendlyServer.Application.Services.Shipping.Contracts;

namespace VendlyServer.Application.Services.Orders;

public interface IOrderService
{
    // Customer — checkout flow
    Task<Result<CreateOrderResponse>> CreateDraftAsync(long userId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result> SetAddressAsync(long userId, long id, SetOrderAddressRequest request, CancellationToken cancellationToken = default);
    Task<Result<ShippingQuoteResponse>> QuoteForAddressAsync(long userId, long addressId, CancellationToken cancellationToken = default);
    Task<Result<List<OrderListItemResponse>>> GetActiveOrdersAsync(long userId, CancellationToken cancellationToken = default);
    Task<Result> CancelDraftAsync(long userId, long orderId, CancellationToken cancellationToken = default);

    // Customer — read
    Task<Result<List<OrderListItemResponse>>> GetMyOrdersAsync(long userId, OrderFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result<OrderResponse>> GetMyByIdAsync(long userId, long id, CancellationToken cancellationToken = default);

    // Admin
    Task<Result<List<OrderListItemResponse>>> GetAllAsync(OrderFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result<OrderResponse>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Result<OrderResponse>> UpdateStatusAsync(long adminId, long id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result<OrderNoteResponse>> AddNoteAsync(long adminId, long id, AddOrderNoteRequest request, CancellationToken cancellationToken = default);
    Task<Result<OrderResponse>> CancelAsync(long adminId, string role, long id, CancelOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result<string>> GetStickerAsync(long id, CancellationToken cancellationToken = default);
}
