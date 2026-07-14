namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// SQL Server / EF Core connection behavior. Bound from the "Database" configuration section.
/// </summary>
public class SqlOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// EF Core command timeout in seconds. Default 30.
    /// Env: Database__CommandTimeoutSeconds
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Max retry count for EnableRetryOnFailure (Azure SQL transient faults). Default 5.
    /// Env: Database__MaxRetryCount
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// Max delay between retries in seconds. Default 30.
    /// Env: Database__MaxRetryDelaySeconds
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// When true, services that support auto-migrate / ensure-created run on startup.
    /// Env: Database__AutoMigrate
    /// </summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>
    /// When true, rewrites the SQL connection string to use
    /// <c>Authentication=Active Directory Default</c> (DefaultAzureCredential / Managed Identity).
    /// When null, follows <c>AzureAuth:PreferManagedIdentity</c> (true in Production).
    /// Env: Database__UseManagedIdentity
    /// </summary>
    public bool? UseManagedIdentity { get; set; }
}

/// <summary>
/// Backward-compatible alias for <see cref="SqlOptions"/>. Prefer SqlOptions in new code.
/// </summary>
[Obsolete("Use SqlOptions instead. AzureSqlOptions is retained for backward compatibility.")]
public class AzureSqlOptions : SqlOptions;
