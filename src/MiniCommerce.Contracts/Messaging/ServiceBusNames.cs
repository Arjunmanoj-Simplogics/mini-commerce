namespace MiniCommerce.Contracts.Messaging;

/// <summary>
/// Shared Azure Service Bus naming conventions for Mini Commerce.
/// </summary>
public static class ServiceBusNames
{
    public const string OrdersTopic = "orders";
    public const string NotificationSubscription = "notification-service";
    public const string InventorySubscription = "inventory-service";

    public const string EventTypeProperty = "EventType";

    public const string OrderCreated = "OrderCreated";
    public const string OrderStatusChanged = "OrderStatusChanged";
    public const string OrderCancelled = "OrderCancelled";
}
