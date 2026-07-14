using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniCommerce.AzureAuth;
using MiniCommerce.Storage.Abstractions;
using MiniCommerce.Storage.Internal;
using MiniCommerce.Storage.Options;

namespace MiniCommerce.Storage.DependencyInjection;

/// <summary>
/// Registers Azure Blob Storage services when <c>BlobStorage:Enabled</c> is true.
/// Instance-based registrar (no static service locator).
/// </summary>
public sealed class BlobStorageServiceRegistrar
{
    /// <summary>
    /// Binds <see cref="BlobStorageOptions"/> and registers <see cref="IBlobStorageService"/>
    /// when blob storage is enabled in configuration.
    /// </summary>
    /// <param name="services">Application service collection.</param>
    /// <param name="configuration">Application configuration (appsettings, env vars, Key Vault).</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMiniCommerceAzureCredential(configuration);
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));

        var options = configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>();
        if (options?.Enabled == true)
        {
            services.AddSingleton<IBlobContainerClientFactory, AzureBlobContainerClientFactory>();
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
        }

        return services;
    }
}
