using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniCommerce.AzureAuth;
using Serilog;

namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Adds Azure Key Vault as an automatic configuration source when <c>KeyVault:Enabled</c> is true.
/// Uses the shared <see cref="AzureCredentialBootstrap"/> / <c>DefaultAzureCredential</c>
/// (Managed Identity in Production; Azure CLI / Visual Studio in Development).
/// </summary>
public static class KeyVaultConfigurationExtensions
{
    /// <summary>
    /// Registers Key Vault options and, when enabled, loads secrets into configuration.
    /// Safe no-op when disabled so local development with User Secrets / appsettings continues to work.
    /// </summary>
    public static WebApplicationBuilder AddKeyVaultConfiguration(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var section = builder.Configuration.GetSection(KeyVaultOptions.SectionName);
        builder.Services.Configure<KeyVaultOptions>(section);

        var options = section.Get<KeyVaultOptions>() ?? new KeyVaultOptions();
        if (!options.Enabled)
        {
            return builder;
        }

        if (string.IsNullOrWhiteSpace(options.VaultUri) ||
            !Uri.TryCreate(options.VaultUri, UriKind.Absolute, out var vaultUri))
        {
            throw new InvalidOperationException(
                "KeyVault:Enabled is true but KeyVault:VaultUri is missing or invalid. " +
                "Set KeyVault__VaultUri=https://{name}.vault.azure.net/");
        }

        // Single credential strategy for the whole application (DefaultAzureCredential)
        var credential = AzureCredentialBootstrap.Create(builder.Configuration, builder.Environment);
        var kvOptions = new AzureKeyVaultConfigurationOptions();
        if (options.ReloadIntervalMinutes > 0)
        {
            kvOptions.ReloadInterval = TimeSpan.FromMinutes(options.ReloadIntervalMinutes);
        }

        builder.Configuration.AddAzureKeyVault(vaultUri, credential, kvOptions);

        Log.Information(
            "Key Vault configuration source added with DefaultAzureCredential. Host={VaultHost} PreferManagedIdentity={PreferManagedIdentity} ReloadMinutes={ReloadMinutes}",
            vaultUri.Host,
            AzureCredentialBootstrap.PreferManagedIdentity(builder.Configuration, builder.Environment),
            options.ReloadIntervalMinutes);

        return builder;
    }
}
