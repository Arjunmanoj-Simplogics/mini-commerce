namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Process hosting options for Kubernetes / containers. Bound from <c>Hosting</c>.
/// Prefer environment variables: <c>Hosting__ShutdownTimeoutSeconds</c>, etc.
/// </summary>
public sealed class HostingOptions
{
    /// <summary>Configuration section: <c>Hosting</c>.</summary>
    public const string SectionName = "Hosting";

    /// <summary>
    /// Time allowed after SIGTERM for in-flight requests and hosted services to finish.
    /// Should be slightly less than the pod <c>terminationGracePeriodSeconds</c>.
    /// Env: <c>Hosting__ShutdownTimeoutSeconds</c>. Default 30.
    /// </summary>
    public int ShutdownTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// When true, enable ASP.NET HTTPS redirection. Keep false behind ingress TLS termination.
    /// Env: <c>Hosting__UseHttpsRedirection</c>.
    /// </summary>
    public bool UseHttpsRedirection { get; set; }

    /// <summary>
    /// Kestrel max request body size in bytes. Default 10 MB.
    /// Env: <c>Hosting__MaxRequestBodySizeBytes</c>.
    /// </summary>
    public long MaxRequestBodySizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Kestrel max concurrent connections (null = unlimited). Default unlimited.
    /// Env: <c>Hosting__MaxConcurrentConnections</c>.
    /// </summary>
    public long? MaxConcurrentConnections { get; set; }

    /// <summary>
    /// Keep-alive timeout in seconds. Default 130 (ASP.NET default).
    /// Env: <c>Hosting__KeepAliveTimeoutSeconds</c>.
    /// </summary>
    public int KeepAliveTimeoutSeconds { get; set; } = 130;

    /// <summary>
    /// Request headers timeout in seconds. Default 30.
    /// Env: <c>Hosting__RequestHeadersTimeoutSeconds</c>.
    /// </summary>
    public int RequestHeadersTimeoutSeconds { get; set; } = 30;
}
