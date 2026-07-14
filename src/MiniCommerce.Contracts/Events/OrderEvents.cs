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

/// <summary>
/// Integration event raised when a payment charge succeeds.
/// </summary>
public sealed class PaymentCompletedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid PaymentId { get; init; }
    public decimal ChargedAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string Last4 { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Integration event raised when inventory stock is reserved for an order.
/// </summary>
public sealed class InventoryReservedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int RemainingQuantity { get; init; }
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Integration event raised when inventory reservation fails (e.g. insufficient stock).
/// </summary>
public sealed class InventoryFailedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}
