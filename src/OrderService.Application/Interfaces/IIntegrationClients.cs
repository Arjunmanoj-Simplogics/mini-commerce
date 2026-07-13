using MiniCommerce.Contracts.Events;
using MiniCommerce.Contracts.Inventory;

namespace OrderService.Application.Interfaces;

/// <summary>
/// HTTP client abstraction for Inventory Service (sync stock reservation).
/// </summary>
public interface IInventoryClient
{
    Task<ReserveStockResponse> ReserveStockAsync(ReserveStockRequest request, CancellationToken cancellationToken = default);
    Task ReleaseStockAsync(ReleaseStockRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes integration events (Azure Service Bus when enabled; HTTP fallback otherwise).
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken cancellationToken = default);
    Task PublishOrderStatusChangedAsync(OrderStatusChangedEvent evt, CancellationToken cancellationToken = default);
    Task PublishOrderCancelledAsync(OrderCancelledEvent evt, CancellationToken cancellationToken = default);
}
