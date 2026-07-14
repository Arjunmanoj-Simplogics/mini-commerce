namespace MiniCommerce.Storage.Options;

/// <summary>
/// Azure Blob Storage configuration bound from the <c>BlobStorage</c> section.
/// Local development uses <see cref="ConnectionString"/>; production uses
/// <see cref="ServiceUri"/> or <see cref="AccountName"/> with Managed Identity.
/// </summary>
public sealed class BlobStorageOptions
{
    /// <summary>Configuration section name: <c>BlobStorage</c>.</summary>
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// When false, blob services are not registered. Env: <c>BlobStorage__Enabled</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Blob service URI, e.g. <c>https://account.blob.core.windows.net</c>.
    /// Env: <c>BlobStorage__ServiceUri</c>.
    /// </summary>
    public string? ServiceUri { get; set; }

    /// <summary>
    /// Storage account name when <see cref="ServiceUri"/> is omitted.
    /// Env: <c>BlobStorage__AccountName</c>.
    /// </summary>
    public string? AccountName { get; set; }

    /// <summary>
    /// Target container for uploads. Env: <c>BlobStorage__ContainerName</c>.
    /// </summary>
    public string ContainerName { get; set; } = "product-images";

    /// <summary>
    /// Connection string for local development only. Do not set in Azure.
    /// Env: <c>BlobStorage__ConnectionString</c>.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>Azure SDK exponential retry count. Default 5.</summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>Initial retry delay in seconds. Default 1.</summary>
    public int RetryDelaySeconds { get; set; } = 1;

    /// <summary>Maximum retry delay in seconds. Default 30.</summary>
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>Per-attempt network timeout in seconds. Default 60.</summary>
    public int NetworkTimeoutSeconds { get; set; } = 60;
}
