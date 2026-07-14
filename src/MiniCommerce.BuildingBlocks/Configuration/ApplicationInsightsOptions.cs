namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Azure Application Insights / Azure Monitor settings.
/// Prefer injecting the connection string from Key Vault or env in Azure.
/// </summary>
public class ApplicationInsightsOptions
{
    public const string SectionName = "ApplicationInsights";

    /// <summary>
    /// Application Insights connection string. Empty disables telemetry registration.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// When true, enables dependency tracking including SQL.
    /// </summary>
    public bool EnableDependencyTracking { get; set; } = true;

    /// <summary>
    /// When true, enables performance counter collection where supported.
    /// </summary>
    public bool EnablePerformanceCounters { get; set; } = true;
}
