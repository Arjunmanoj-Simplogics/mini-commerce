using Azure.Identity;

namespace OrderService.API.Configuration;

/// <summary>
/// Configuration options for Azure Key Vault integration.
/// </summary>
public class KeyVaultOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "KeyVault";

    /// <summary>
    /// Gets or sets the Key Vault URI.
    /// </summary>
    public string? VaultUri { get; set; }

    /// <summary>
    /// Gets or sets whether Key Vault integration is enabled.
    /// </summary>
    public bool Enabled { get; set; }
}

/// <summary>
/// Extension methods for Azure Key Vault configuration.
/// </summary>
public static class KeyVaultConfigurationExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source when enabled.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder.</returns>
    public static WebApplicationBuilder AddKeyVaultConfiguration(this WebApplicationBuilder builder)
    {
        var keyVaultSection = builder.Configuration.GetSection(KeyVaultOptions.SectionName);
        builder.Services.Configure<KeyVaultOptions>(keyVaultSection);

        var options = keyVaultSection.Get<KeyVaultOptions>();
        if (options?.Enabled == true && !string.IsNullOrWhiteSpace(options.VaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(options.VaultUri),
                new DefaultAzureCredential());
        }

        return builder;
    }
}
