namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Azure Key Vault configuration. When <see cref="Enabled"/> is true, secrets are loaded
/// via the shared <c>DefaultAzureCredential</c> provider (<see cref="MiniCommerce.AzureAuth"/>).
/// Local development keeps Enabled=false and uses User Secrets / appsettings.
/// Bound from <c>KeyVault</c>. Env: <c>KeyVault__Enabled</c>, <c>KeyVault__VaultUri</c>.
/// </summary>
public sealed class KeyVaultOptions
{
    /// <summary>Configuration section name: <c>KeyVault</c>.</summary>
    public const string SectionName = "KeyVault";

    /// <summary>
    /// Key Vault URI, e.g. <c>https://my-vault.vault.azure.net/</c>.
    /// Env: <c>KeyVault__VaultUri</c>.
    /// </summary>
    public string? VaultUri { get; set; }

    /// <summary>
    /// When true, add Key Vault as a configuration source.
    /// Env: <c>KeyVault__Enabled</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Obsolete — use <c>AzureAuth:PreferManagedIdentity</c> / <c>AzureAuth:ManagedIdentityClientId</c>.
    /// Retained so existing appsettings bind without breaking.
    /// </summary>
    [Obsolete("Use AzureAuth:PreferManagedIdentity. Credential is always DefaultAzureCredential.")]
    public bool? UseManagedIdentity { get; set; }

    /// <summary>
    /// Obsolete — use <c>AzureAuth:ManagedIdentityClientId</c>.
    /// </summary>
    [Obsolete("Use AzureAuth:ManagedIdentityClientId.")]
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// How often to reload secrets from Key Vault (minutes). Default 30. Set 0 to disable reload.
    /// Env: <c>KeyVault__ReloadIntervalMinutes</c>.
    /// </summary>
    public int ReloadIntervalMinutes { get; set; } = 30;
}

/// <summary>
/// Canonical Key Vault secret names. Key Vault does not allow <c>:</c> in names;
/// use <c>--</c> which the configuration provider maps to nested keys (<c>:</c>).
/// </summary>
public static class KeyVaultSecretNames
{
    // --- JWT ---
    public const string JwtSigningKey = "Jwt--SigningKey";
    public const string JwtIssuer = "Jwt--Issuer";
    public const string JwtAudience = "Jwt--Audience";

    // --- Connection strings ---
    public const string ConnectionStringsOrderDB = "ConnectionStrings--OrderDB";
    public const string ConnectionStringsInventoryDB = "ConnectionStrings--InventoryDB";
    public const string ConnectionStringsNotificationDB = "ConnectionStrings--NotificationDB";
    public const string ConnectionStringsAuthDB = "ConnectionStrings--AuthDB";
    public const string ConnectionStringsCatalogDB = "ConnectionStrings--CatalogDB";
    public const string ConnectionStringsCartDB = "ConnectionStrings--CartDB";

    // --- Service Bus ---
    public const string ServiceBusConnectionString = "ServiceBus--ConnectionString";
    public const string ServiceBusFullyQualifiedNamespace = "ServiceBus--FullyQualifiedNamespace";

    // --- Blob Storage ---
    public const string BlobStorageConnectionString = "BlobStorage--ConnectionString";
    public const string BlobStorageServiceUri = "BlobStorage--ServiceUri";
    public const string BlobStorageAccountName = "BlobStorage--AccountName";

    // --- Application Insights / API keys ---
    public const string ApplicationInsightsConnectionString = "ApplicationInsights--ConnectionString";
    public const string ApiKeysInternal = "ApiKeys--Internal";

    /// <summary>
    /// All secrets expected for a full Mini Commerce deployment.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        JwtSigningKey,
        JwtIssuer,
        JwtAudience,
        ConnectionStringsOrderDB,
        ConnectionStringsInventoryDB,
        ConnectionStringsNotificationDB,
        ConnectionStringsAuthDB,
        ConnectionStringsCatalogDB,
        ConnectionStringsCartDB,
        ServiceBusConnectionString,
        ServiceBusFullyQualifiedNamespace,
        BlobStorageConnectionString,
        BlobStorageServiceUri,
        BlobStorageAccountName,
        ApplicationInsightsConnectionString,
        ApiKeysInternal
    ];
}
