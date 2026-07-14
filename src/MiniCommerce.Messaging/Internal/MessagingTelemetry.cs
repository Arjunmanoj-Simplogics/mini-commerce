using System.Diagnostics;

namespace MiniCommerce.Messaging.Internal;

/// <summary>
/// ActivitySource for Azure Service Bus publish/consume spans (registered via BuildingBlocks OTel).
/// </summary>
internal static class MessagingTelemetry
{
    public const string SourceName = "MiniCommerce.Messaging";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
