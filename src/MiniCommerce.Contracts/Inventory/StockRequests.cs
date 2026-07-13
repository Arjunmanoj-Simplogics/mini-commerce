namespace MiniCommerce.Contracts.Inventory;

/// <summary>
/// Request to reserve inventory stock for an order.
/// </summary>
public sealed class ReserveStockRequest
{
    public string ProductSku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public Guid OrderId { get; init; }
}

/// <summary>
/// Result of a stock reservation attempt.
/// </summary>
public sealed class ReserveStockResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int RemainingQuantity { get; init; }
}

/// <summary>
/// Request to release previously reserved inventory.
/// </summary>
public sealed class ReleaseStockRequest
{
    public string ProductSku { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public Guid OrderId { get; init; }
}
