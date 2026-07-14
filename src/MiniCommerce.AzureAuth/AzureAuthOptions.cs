namespace MiniCommerce.AzureAuth;

/// <summary>
/// Shared Azure authentication options. Bound from the <c>AzureAuth</c> configuration section.
/// Controls how <see cref="Azure.Identity.DefaultAzureCredential"/> resolves Managed Identity in production
/// while Development continues to use connection strings for Azure services.
/// </summary>
public sealed class AzureAuthOptions
{
    /// <summary>Configuration section name: <c>AzureAuth</c>.</summary>
    public const string SectionName = "AzureAuth";

    /// <summary>
    /// When true, prefer Managed Identity paths for Azure services (Blob URI, Service Bus namespace,
    /// Azure SQL AAD auth). When null, defaults to true in Production and false in Development.
    /// Env: <c>AzureAuth__PreferManagedIdentity</c>.
    /// </summary>
    public bool? PreferManagedIdentity { get; set; }

    /// <summary>
    /// Optional user-assigned managed identity / workload identity client id passed to
    /// <c>DefaultAzureCredential</c>. Leave empty for system-assigned MI.
    /// Env: <c>AzureAuth__ManagedIdentityClientId</c>.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }
}
