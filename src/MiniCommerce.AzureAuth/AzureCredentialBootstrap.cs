using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MiniCommerce.AzureAuth;

/// <summary>
/// Creates <see cref="DefaultAzureCredential"/> before the DI container is built
/// (required for Key Vault configuration loading at startup).
/// Runtime services must prefer <see cref="IAzureCredentialProvider"/> from DI.
/// </summary>
public static class AzureCredentialBootstrap
{
    /// <summary>
    /// Builds the same <see cref="DefaultAzureCredential"/> used by <see cref="AzureCredentialProvider"/>.
    /// </summary>
    public static TokenCredential Create(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var settings = configuration.GetSection(AzureAuthOptions.SectionName).Get<AzureAuthOptions>()
            ?? new AzureAuthOptions();

        var dacOptions = new DefaultAzureCredentialOptions
        {
            ExcludeInteractiveBrowserCredential = true
        };

        if (!string.IsNullOrWhiteSpace(settings.ManagedIdentityClientId))
        {
            dacOptions.ManagedIdentityClientId = settings.ManagedIdentityClientId;
        }

        return new DefaultAzureCredential(dacOptions);
    }

    /// <summary>
    /// Resolves whether Managed Identity style endpoints should be preferred.
    /// </summary>
    public static bool PreferManagedIdentity(IConfiguration configuration, IHostEnvironment environment)
    {
        var settings = configuration.GetSection(AzureAuthOptions.SectionName).Get<AzureAuthOptions>()
            ?? new AzureAuthOptions();
        return settings.PreferManagedIdentity ?? environment.IsProduction();
    }
}
