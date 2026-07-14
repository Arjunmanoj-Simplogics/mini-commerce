using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MiniCommerce.BuildingBlocks.Observability;

/// <summary>
/// Shared ActivitySource / Meter names for Mini Commerce custom instrumentation
/// (Service Bus, Blob Storage) and Azure SDK sources.
/// </summary>
public static class MiniCommerceTelemetry
{
    /// <summary>Root ActivitySource for application-level spans.</summary>
    public const string ActivitySourceName = "MiniCommerce";

    /// <summary>Service Bus publish/consume ActivitySource.</summary>
    public const string ServiceBusSourceName = "MiniCommerce.Messaging";

    /// <summary>Blob Storage ActivitySource.</summary>
    public const string StorageSourceName = "MiniCommerce.Storage";

    /// <summary>Shared meter for custom metrics (reserved for future counters).</summary>
    public const string MeterName = "MiniCommerce";

    /// <summary>Listen to all Azure SDK ActivitySources (Blob, Service Bus, …).</summary>
    public const string AzureSourceName = "Azure.*";

    private static readonly ActivitySource RootSource = new(ActivitySourceName);
    private static readonly ActivitySource MessagingSource = new(ServiceBusSourceName);
    private static readonly ActivitySource StorageSource = new(StorageSourceName);
    private static readonly Meter RootMeter = new(MeterName);

    /// <summary>Application ActivitySource.</summary>
    public static ActivitySource ActivitySource => RootSource;

    /// <summary>Messaging ActivitySource.</summary>
    public static ActivitySource Messaging => MessagingSource;

    /// <summary>Storage ActivitySource.</summary>
    public static ActivitySource Storage => StorageSource;

    /// <summary>Application Meter.</summary>
    public static Meter Meter => RootMeter;
}
