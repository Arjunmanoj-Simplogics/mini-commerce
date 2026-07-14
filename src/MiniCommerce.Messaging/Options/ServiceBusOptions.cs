namespace MiniCommerce.Messaging.Options;

/// <summary>
/// Azure Service Bus configuration bound from the <c>ServiceBus</c> section.
/// Local development uses <see cref="ConnectionString"/>; production uses
/// <see cref="FullyQualifiedNamespace"/> with Managed Identity.
/// </summary>
public sealed class ServiceBusOptions
{
    /// <summary>Configuration section name: <c>ServiceBus</c>.</summary>
    public const string SectionName = "ServiceBus";

    /// <summary>
    /// When true, publishers send to Service Bus and consumers start.
    /// When false, Order uses HTTP fallback; consumers exit without processing.
    /// Env: <c>ServiceBus__Enabled</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Connection string for local development. Leave empty in Azure.
    /// Env: <c>ServiceBus__ConnectionString</c>.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Namespace host, e.g. <c>myminibus.servicebus.windows.net</c> (Managed Identity).
    /// Env: <c>ServiceBus__FullyQualifiedNamespace</c>.
    /// </summary>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Topic name for integration events. Env: <c>ServiceBus__TopicName</c>.
    /// </summary>
    public string TopicName { get; set; } = "orders";

    /// <summary>
    /// Subscription name for the consuming service. Env: <c>ServiceBus__SubscriptionName</c>.
    /// </summary>
    public string SubscriptionName { get; set; } = "notification-service";

    /// <summary>SDK exponential retry count. Default 5.</summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>Initial retry delay in seconds. Default 1.</summary>
    public int RetryDelaySeconds { get; set; } = 1;

    /// <summary>Maximum retry delay in seconds. Default 30.</summary>
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>Maximum concurrent message handlers for the processor. Default 4.</summary>
    public int MaxConcurrentCalls { get; set; } = 4;

    /// <summary>Auto-lock renewal duration in minutes. Default 5.</summary>
    public int MaxAutoLockRenewalMinutes { get; set; } = 5;
}
