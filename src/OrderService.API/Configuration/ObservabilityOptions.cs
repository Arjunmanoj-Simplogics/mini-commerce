namespace OrderService.API.Configuration;

/// <summary>
/// Configuration options for future OpenTelemetry and Azure Monitor integration.
/// </summary>
public class ObservabilityOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Observability";

    /// <summary>
    /// Gets or sets whether distributed tracing is enabled.
    /// </summary>
    public bool EnableTracing { get; set; }

    /// <summary>
    /// Gets or sets the Application Insights connection string.
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the OTLP exporter endpoint.
    /// </summary>
    public string? OtlpEndpoint { get; set; }
}
