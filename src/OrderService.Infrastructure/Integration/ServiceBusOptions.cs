namespace OrderService.Infrastructure.Integration;

/// <summary>
/// Azure Service Bus configuration for Order Service publishers.
/// </summary>
public class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    /// <summary>
    /// When true, publishes via Azure Service Bus; otherwise uses HTTP fallback.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Service Bus connection string (prefer env var / secret).
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Topic name for order integration events.
    /// </summary>
    public string TopicName { get; set; } = MiniCommerce.Contracts.Messaging.ServiceBusNames.OrdersTopic;
}
