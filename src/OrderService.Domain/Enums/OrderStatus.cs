namespace OrderService.Domain.Enums;

/// <summary>
/// Defines the lifecycle states of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order has been created and is awaiting processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Order is being prepared or fulfilled.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Order has been shipped to the customer.
    /// </summary>
    Shipped = 2,

    /// <summary>
    /// Order has been delivered successfully.
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// Order has been cancelled.
    /// </summary>
    Cancelled = 4
}
