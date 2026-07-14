using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MiniCommerce.AzureAuth;
using MiniCommerce.BuildingBlocks.Configuration;

namespace MiniCommerce.BuildingBlocks.Health;

/// <summary>
/// Readiness check for Azure Key Vault (registered only when KeyVault:Enabled=true).
/// Uses the shared <see cref="IAzureCredentialProvider"/> (DefaultAzureCredential).
/// </summary>
public sealed class KeyVaultHealthCheck : IHealthCheck
{
    private readonly KeyVaultOptions _options;
    private readonly IAzureCredentialProvider _credentialProvider;

    public KeyVaultHealthCheck(IOptions<KeyVaultOptions> options, IAzureCredentialProvider credentialProvider)
    {
        _options = options.Value;
        _credentialProvider = credentialProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.VaultUri))
        {
            return HealthCheckResult.Unhealthy("Key Vault is enabled but KeyVault:VaultUri is not configured.");
        }

        if (!Uri.TryCreate(_options.VaultUri, UriKind.Absolute, out var vaultUri))
        {
            return HealthCheckResult.Unhealthy($"Key Vault VaultUri is not a valid absolute URI: '{_options.VaultUri}'.");
        }

        try
        {
            var client = new SecretClient(vaultUri, _credentialProvider.Credential);

            await foreach (var _ in client.GetPropertiesOfSecretsAsync(cancellationToken).WithCancellation(cancellationToken))
            {
                break;
            }

            return HealthCheckResult.Healthy("Key Vault is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Key Vault unhealthy.", ex);
        }
    }
}
