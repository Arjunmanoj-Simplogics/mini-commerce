using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MiniCommerce.AzureAuth;
using MiniCommerce.BuildingBlocks.Configuration;
using MiniCommerce.BuildingBlocks.Data;
using MiniCommerce.Messaging.Options;
using MiniCommerce.Storage.Options;

namespace MiniCommerce.BuildingBlocks.Health;

/// <summary>
/// Shared ASP.NET Core Health Checks registration and endpoint mapping.
/// </summary>
/// <remarks>
/// <para>
/// <b>/health/live</b> — process liveness (tag: <see cref="LiveTag"/>). Used by Docker/K8s liveness probes.
/// </para>
/// <para>
/// <b>/health/ready</b> — dependency readiness (tag: <see cref="ReadyTag"/>): SQL, Service Bus, Blob, Key Vault
/// when the corresponding feature is configured/enabled.
/// </para>
/// </remarks>
public static class HealthCheckExtensions
{
    public const string LiveTag = "live";
    public const string ReadyTag = "ready";

    /// <summary>
    /// Registers the process <c>self</c> liveness check and conditional readiness checks:
    /// SQL (when <paramref name="sqlConnectionString"/> is set), Blob Storage, Service Bus, and Key Vault.
    /// </summary>
    public static IHealthChecksBuilder AddMiniCommerceHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sqlConnectionString = null,
        string sqlCheckName = "sql")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Shared DefaultAzureCredential for Key Vault / Service Bus readiness probes
        services.AddMiniCommerceAzureCredential(configuration);

        var builder = services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("Process is running."),
                tags: [LiveTag, ReadyTag]);

        // --- SQL Server (ready) ---
        if (!string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            var sqlOptions = configuration.GetSection(SqlOptions.SectionName).Get<SqlOptions>()
                ?? new SqlOptions();
            var preferMi = sqlOptions.UseManagedIdentity
                ?? configuration.GetSection(AzureAuthOptions.SectionName).Get<AzureAuthOptions>()?.PreferManagedIdentity
                ?? string.Equals(
                    configuration["ASPNETCORE_ENVIRONMENT"]
                        ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    "Production",
                    StringComparison.OrdinalIgnoreCase);

            var effectiveSql = AzureSqlExtensions.ApplyManagedIdentityIfNeeded(sqlConnectionString, preferMi);

            builder.AddSqlServer(
                connectionString: effectiveSql,
                name: sqlCheckName,
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag],
                timeout: TimeSpan.FromSeconds(5));
        }

        // --- Azure Blob Storage (ready when enabled) ---
        var blob = configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>()
            ?? new BlobStorageOptions();
        if (blob.Enabled)
        {
            builder.AddCheck<BlobStorageHealthCheck>(
                name: "blob",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag],
                timeout: TimeSpan.FromSeconds(10));
        }

        // --- Azure Service Bus (ready when enabled) ---
        var serviceBus = configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>()
            ?? new ServiceBusOptions();
        if (serviceBus.Enabled)
        {
            builder.AddCheck<ServiceBusHealthCheck>(
                name: "servicebus",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag],
                timeout: TimeSpan.FromSeconds(10));
        }

        // --- Azure Key Vault (ready when enabled) ---
        var keyVault = configuration.GetSection(KeyVaultOptions.SectionName).Get<KeyVaultOptions>()
            ?? new KeyVaultOptions();
        if (keyVault.Enabled)
        {
            builder.AddCheck<KeyVaultHealthCheck>(
                name: "keyvault",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag],
                timeout: TimeSpan.FromSeconds(10));
        }

        return builder;
    }

    /// <summary>
    /// Maps health endpoints with standard JSON responses. No authentication required (probe-friendly).
    /// </summary>
    /// <remarks>
    /// Primary paths: <c>/health</c>, <c>/health/live</c>, <c>/health/ready</c>.
    /// Legacy aliases: <c>/api/health*</c> (kept for existing Docker/K8s manifests).
    /// </remarks>
    public static WebApplication MapMiniCommerceHealthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        MapJsonHealth(app, "/health", _ => true);
        MapJsonHealth(app, "/health/live", check => check.Tags.Contains(LiveTag));
        MapJsonHealth(app, "/health/ready", check => check.Tags.Contains(ReadyTag));

        // Backward-compatible probe paths used by existing Kubernetes deployments
        MapJsonHealth(app, "/api/health", _ => true);
        MapJsonHealth(app, "/api/health/live", check => check.Tags.Contains(LiveTag));
        MapJsonHealth(app, "/api/health/ready", check => check.Tags.Contains(ReadyTag));

        return app;
    }

    private static void MapJsonHealth(WebApplication app, string path, Func<HealthCheckRegistration, bool> predicate)
    {
        app.MapHealthChecks(path, new HealthCheckOptions
        {
            Predicate = predicate,
            ResponseWriter = HealthCheckResponseWriter.WriteAsync,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        }).AllowAnonymous();
    }
}
