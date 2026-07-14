namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Internal HTTP base URLs for service-to-service calls (Order → Inventory / Notification).
/// Bound from the "Services" configuration section.
/// Kubernetes: set via env, e.g. Services__Inventory=http://inventory-service:8080
/// (never bake localhost into Production images).
/// </summary>
public class DownstreamServicesOptions
{
    public const string SectionName = "Services";

    /// <summary>
    /// Inventory service base URL. Empty until configured.
    /// Env: Services__Inventory
    /// </summary>
    public string Inventory { get; set; } = string.Empty;

    /// <summary>
    /// Notification service base URL. Empty until configured.
    /// Env: Services__Notification
    /// </summary>
    public string Notification { get; set; } = string.Empty;

    /// <summary>Default HttpClient timeout in seconds for downstream calls.</summary>
    public int HttpClientTimeoutSeconds { get; set; } = 30;
}
