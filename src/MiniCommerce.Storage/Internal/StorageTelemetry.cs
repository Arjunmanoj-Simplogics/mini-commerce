using System.Diagnostics;

namespace MiniCommerce.Storage.Internal;

/// <summary>
/// ActivitySource for Azure Blob Storage spans (registered via BuildingBlocks OTel).
/// </summary>
internal static class StorageTelemetry
{
    public const string SourceName = "MiniCommerce.Storage";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}
