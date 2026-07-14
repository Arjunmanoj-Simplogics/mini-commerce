using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MiniCommerce.BuildingBlocks.Configuration;

namespace MiniCommerce.BuildingBlocks.Hosting;

/// <summary>
/// Kubernetes / AKS readiness: Kestrel, forwarded headers, graceful SIGTERM shutdown.
/// URLs and secrets come from environment variables (ConfigMap / Secret), not localhost defaults.
/// </summary>
public static class AksHostingExtensions
{
    /// <summary>
    /// Configures Kestrel, <see cref="HostOptions.ShutdownTimeout"/> for SIGTERM drain,
    /// and forwarded headers for ingress TLS termination.
    /// </summary>
    /// <remarks>
    /// Listen addresses are controlled solely by <c>ASPNETCORE_URLS</c>
    /// (e.g. <c>http://+:8080</c>) — do not hardcode localhost ports in code.
    /// </remarks>
    public static WebApplicationBuilder AddMiniCommerceAksHosting(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<HostingOptions>(
            builder.Configuration.GetSection(HostingOptions.SectionName));

        var hosting = builder.Configuration.GetSection(HostingOptions.SectionName).Get<HostingOptions>()
            ?? new HostingOptions();

        var shutdownSeconds = Math.Max(5, hosting.ShutdownTimeoutSeconds);

        // Graceful shutdown: in-flight HTTP + hosted services (e.g. Service Bus) drain on SIGTERM
        builder.Services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(shutdownSeconds);
        });

        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            ConfigureKestrel(options, hosting);
        });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            // Trust cluster ingress / load balancer (Azure AGIC, nginx, Front Door)
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            options.RequireHeaderSymmetry = false;
            options.ForwardLimit = null; // allow multi-hop ingress → service mesh
        });

        return builder;
    }

    /// <summary>
    /// Applies forwarded headers early in the pipeline (before HTTPS / auth decisions).
    /// </summary>
    public static WebApplication UseMiniCommerceForwardedHeaders(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseForwardedHeaders();
        return app;
    }

    /// <summary>
    /// Optional HTTPS redirection — off by default for Kubernetes (TLS at ingress).
    /// Enable via <c>Hosting__UseHttpsRedirection=true</c> when the app terminates TLS itself.
    /// </summary>
    public static WebApplication UseMiniCommerceHttpsRedirection(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var hosting = app.Configuration.GetSection(HostingOptions.SectionName).Get<HostingOptions>()
            ?? new HostingOptions();

        if (hosting.UseHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        return app;
    }

    private static void ConfigureKestrel(KestrelServerOptions options, HostingOptions hosting)
    {
        // Hardening for multi-replica pods behind a load balancer
        options.AddServerHeader = false;
        options.AllowSynchronousIO = false;

        options.Limits.MaxConcurrentConnections = hosting.MaxConcurrentConnections;
        options.Limits.MaxConcurrentUpgradedConnections = hosting.MaxConcurrentConnections;
        options.Limits.MaxRequestBodySize = hosting.MaxRequestBodySizeBytes;
        options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(hosting.KeepAliveTimeoutSeconds);
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(hosting.RequestHeadersTimeoutSeconds);

        // Do not call Listen/ListenAnyIP here — ASPNETCORE_URLS owns binding (port 8080 in containers).
    }
}
