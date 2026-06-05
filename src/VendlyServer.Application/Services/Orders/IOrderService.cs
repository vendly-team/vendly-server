using VendlyServer.Domain.Abstractions;
using VendlyServer.Application.Services.Orders.Contracts;

namespace VendlyServer.Application.Services.Orders;

public interface IOrderService
{
    // Customer
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
