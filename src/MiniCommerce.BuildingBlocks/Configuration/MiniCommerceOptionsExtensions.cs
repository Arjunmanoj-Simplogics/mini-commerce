using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniCommerce.BuildingBlocks.Auth;
using MiniCommerce.Storage.Options;
using MiniCommerce.Messaging.Options;

namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Central registration for all strongly typed Mini Commerce configuration options.
/// All options bind from IConfiguration and support environment variable overrides (section__key).
/// </summary>
public static class MiniCommerceOptionsExtensions
{
    /// <summary>
    /// Registers SqlOptions, ConnectionStringsOptions, JwtOptions, BlobStorageOptions,
    /// ServiceBusOptions, KeyVaultOptions, CorsOptions, ApplicationInsightsOptions,
    /// OpenTelemetryOptions, and DownstreamServicesOptions via IOptions&lt;T&gt;.
    /// </summary>
    public static IServiceCollection AddMiniCommerceOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SqlOptions>(configuration.GetSection(SqlOptions.SectionName));
        services.Configure<ConnectionStringsOptions>(configuration.GetSection(ConnectionStringsOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<KeyVaultOptions>(configuration.GetSection(KeyVaultOptions.SectionName));
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));
        services.Configure<ApplicationInsightsOptions>(configuration.GetSection(ApplicationInsightsOptions.SectionName));
        services.Configure<OpenTelemetryOptions>(configuration.GetSection(OpenTelemetryOptions.SectionName));
        services.Configure<HostingOptions>(configuration.GetSection(HostingOptions.SectionName));
        services.Configure<DownstreamServicesOptions>(configuration.GetSection(DownstreamServicesOptions.SectionName));
        return services;
    }

    /// <summary>
    /// Backward-compatible alias for <see cref="AddMiniCommerceOptions"/>.
    /// </summary>
    public static IServiceCollection AddMiniCommerceAzureOptions(this IServiceCollection services, IConfiguration configuration)
        => services.AddMiniCommerceOptions(configuration);
}
