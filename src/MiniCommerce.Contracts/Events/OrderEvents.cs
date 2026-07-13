namespace MiniCommerce.Contracts.Events;

/// <summary>
/// Integration event raised when an order is created.
/// </summary>
public sealed class OrderCreatedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string ProductSku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Integration event raised when an order status changes.
/// </summary>
public sealed class OrderStatusChangedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Integration event raised when an order is cancelled or deleted.
/// </summary>
public sealed class OrderCancelledEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string ProductSku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}
