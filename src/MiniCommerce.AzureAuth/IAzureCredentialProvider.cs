using Azure.Core;

namespace MiniCommerce.AzureAuth;

/// <summary>
/// Provides a single shared <see cref="TokenCredential"/> for all Azure SDK clients
/// (Blob Storage, Key Vault, Service Bus, Azure SQL AAD). Implementations must use
/// <c>DefaultAzureCredential</c> so Managed Identity, Azure CLI, and Visual Studio share one chain.
/// </summary>
public interface IAzureCredentialProvider
{
    /// <summary>
    /// Shared credential instance (singleton). Never create ad-hoc credentials in service code.
    /// </summary>
    TokenCredential Credential { get; }

    /// <summary>
    /// True when the host should prefer Managed Identity style endpoints
    /// (no connection strings) for Azure PaaS services.
    /// </summary>
    bool PreferManagedIdentity { get; }
}
