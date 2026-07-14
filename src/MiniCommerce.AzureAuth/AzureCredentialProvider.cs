using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MiniCommerce.AzureAuth;

/// <summary>
/// Canonical credential provider. Always builds <see cref="DefaultAzureCredential"/>,
/// which includes Managed Identity in Production/AKS and Azure CLI / Visual Studio locally.
/// </summary>
public sealed class AzureCredentialProvider : IAzureCredentialProvider
{
    private readonly Lazy<TokenCredential> _credential;
    private readonly bool _preferManagedIdentity;

    /// <summary>
    /// Creates the provider from options and hosting environment.
    /// </summary>
    public AzureCredentialProvider(
        IOptions<AzureAuthOptions> options,
        IHostEnvironment environment,
        ILogger<AzureCredentialProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        var settings = options.Value;
        _preferManagedIdentity = settings.PreferManagedIdentity ?? environment.IsProduction();

        _credential = new Lazy<TokenCredential>(() =>
        {
            var dacOptions = new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = true
            };

            if (!string.IsNullOrWhiteSpace(settings.ManagedIdentityClientId))
            {
                dacOptions.ManagedIdentityClientId = settings.ManagedIdentityClientId;
            }

            logger.LogInformation(
                "Azure credential provider initialized with DefaultAzureCredential. PreferManagedIdentity={PreferManagedIdentity} HasUserAssignedClientId={HasClientId}",
                _preferManagedIdentity,
                !string.IsNullOrWhiteSpace(settings.ManagedIdentityClientId));

            return new DefaultAzureCredential(dacOptions);
        });
    }

    /// <inheritdoc />
    public TokenCredential Credential => _credential.Value;

    /// <inheritdoc />
    public bool PreferManagedIdentity => _preferManagedIdentity;
}
