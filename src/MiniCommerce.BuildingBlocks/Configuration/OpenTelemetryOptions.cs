namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// OpenTelemetry configuration. Bound from the <c>OpenTelemetry</c> section.
/// Tracing, metrics, and logs export to Console, OTLP, and optionally Azure Monitor.
/// </summary>
public sealed class OpenTelemetryOptions
{
    /// <summary>Configuration section name: <c>OpenTelemetry</c>.</summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// Master switch. When false, only Azure Monitor may still register if its connection string is set
    /// and <see cref="Exporters"/>.<see cref="OpenTelemetryExporterOptions.AzureMonitor"/> is true.
    /// Env: <c>OpenTelemetry__Enabled</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Logical service name for the resource attribute <c>service.name</c>.
    /// Falls back to <c>ApplicationName</c> / assembly name when empty.
    /// Env: <c>OpenTelemetry__ServiceName</c>.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Optional service version resource attribute.
    /// Env: <c>OpenTelemetry__ServiceVersion</c>.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// OTLP endpoint (e.g. <c>http://localhost:4317</c> for gRPC or <c>http://localhost:4318</c> for HTTP).
    /// Env: <c>OpenTelemetry__OtlpEndpoint</c>.
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// OTLP protocol: <c>Grpc</c> or <c>HttpProtobuf</c>. Default Grpc.
    /// Env: <c>OpenTelemetry__OtlpProtocol</c>.
    /// </summary>
    public string OtlpProtocol { get; set; } = "Grpc";

    /// <summary>
    /// When true, skip creating spans for health-check paths (<c>/health*</c>, <c>/api/health*</c>).
    /// Env: <c>OpenTelemetry__ExcludeHealthChecks</c>.
    /// </summary>
    public bool ExcludeHealthChecks { get; set; } = true;

    /// <summary>
    /// When true, include SQL text for SqlClient instrumentation (useful in Dev; careful in Prod).
    /// Env: <c>OpenTelemetry__CaptureSqlText</c>.
    /// </summary>
    public bool CaptureSqlText { get; set; }

    /// <summary>Exporter toggles.</summary>
    public OpenTelemetryExporterOptions Exporters { get; set; } = new();
}

/// <summary>
/// OpenTelemetry exporter configuration nested under <c>OpenTelemetry:Exporters</c>.
/// </summary>
public sealed class OpenTelemetryExporterOptions
{
    /// <summary>
    /// Export traces/metrics/logs to the console (local debugging). Default false for production.
    /// Env: <c>OpenTelemetry__Exporters__Console</c>.
    /// </summary>
    public bool Console { get; set; }

    /// <summary>
    /// Export via OTLP to <see cref="OpenTelemetryOptions.OtlpEndpoint"/>.
    /// Env: <c>OpenTelemetry__Exporters__Otlp</c>.
    /// </summary>
    public bool Otlp { get; set; }

    /// <summary>
    /// Export to Azure Monitor / Application Insights when a connection string is configured.
    /// Env: <c>OpenTelemetry__Exporters__AzureMonitor</c>.
    /// </summary>
    public bool AzureMonitor { get; set; } = true;
}
