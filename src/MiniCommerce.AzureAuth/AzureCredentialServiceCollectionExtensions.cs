using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiniCommerce.AzureAuth;

/// <summary>
/// Registers the shared Azure credential provider used by Blob, Key Vault, Service Bus, and Azure SQL.
/// </summary>
public static class AzureCredentialServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="AzureAuthOptions"/> and registers a singleton
    /// <see cref="IAzureCredentialProvider"/> / <see cref="TokenCredential"/>.
    /// Call early in <c>Program.cs</c> (before Key Vault / Blob / Service Bus registration).
    /// Safe to call multiple times (TryAdd).
    /// </summary>
    public static IServiceCollection AddMiniCommerceAzureCredential(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AzureAuthOptions>(configuration.GetSection(AzureAuthOptions.SectionName));
        services.TryAddSingleton<IAzureCredentialProvider, AzureCredentialProvider>();
        services.TryAddSingleton<TokenCredential>(sp => sp.GetRequiredService<IAzureCredentialProvider>().Credential);
        return services;
    }
}
