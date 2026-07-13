namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Azure Service Bus configuration for Notification Service consumers.
/// </summary>
public class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public bool Enabled { get; set; }

    public string ConnectionString { get; set; } = string.Empty;

    public string TopicName { get; set; } = MiniCommerce.Contracts.Messaging.ServiceBusNames.OrdersTopic;

    public string SubscriptionName { get; set; } = MiniCommerce.Contracts.Messaging.ServiceBusNames.NotificationSubscription;
}
