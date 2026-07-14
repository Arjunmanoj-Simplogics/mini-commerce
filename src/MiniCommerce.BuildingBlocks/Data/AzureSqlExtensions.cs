using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniCommerce.AzureAuth;
using MiniCommerce.BuildingBlocks.Configuration;

namespace MiniCommerce.BuildingBlocks.Data;

/// <summary>
/// Shared EF Core / Azure SQL registration helpers (retry + command timeout from options).
/// Development: SQL authentication via connection string.
/// Production: Managed Identity via <c>Authentication=Active Directory Default</c> (DefaultAzureCredential).
/// </summary>
public static class AzureSqlExtensions
{
    /// <summary>
    /// Configures SQL Server with EnableRetryOnFailure and CommandTimeout from Database options.
    /// Applies Managed Identity (AAD Default) when preferred.
    /// </summary>
    public static DbContextOptionsBuilder UseAzureSqlServer(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        IConfiguration configuration,
        string? migrationsAssembly = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var sqlOptions = configuration.GetSection(SqlOptions.SectionName).Get<SqlOptions>()
            ?? new SqlOptions();

        var preferMi = ResolvePreferManagedIdentity(sqlOptions, configuration);
        var effectiveConnectionString = ApplyManagedIdentityIfNeeded(connectionString, preferMi);

        var maxRetry = Math.Max(1, sqlOptions.MaxRetryCount);
        var maxDelay = TimeSpan.FromSeconds(Math.Max(1, sqlOptions.MaxRetryDelaySeconds));

        return optionsBuilder.UseSqlServer(effectiveConnectionString, sql =>
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: maxRetry,
                maxRetryDelay: maxDelay,
                errorNumbersToAdd: null);
            sql.CommandTimeout(Math.Max(1, sqlOptions.CommandTimeoutSeconds));
            if (!string.IsNullOrWhiteSpace(migrationsAssembly))
            {
                sql.MigrationsAssembly(migrationsAssembly);
            }
        });
    }

    /// <summary>
    /// Opens a raw Microsoft.Data.SqlClient connection to verify connectivity (for startup / health diagnostics).
    /// </summary>
    public static async Task VerifySqlConnectivityAsync(
        string connectionString,
        ILogger logger,
        string databaseName,
        CancellationToken cancellationToken = default,
        IConfiguration? configuration = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var preferMi = configuration is null
            ? false
            : ResolvePreferManagedIdentity(
                configuration.GetSection(SqlOptions.SectionName).Get<SqlOptions>() ?? new SqlOptions(),
                configuration);

        var effective = ApplyManagedIdentityIfNeeded(connectionString, preferMi);
        await using var connection = new SqlConnection(effective);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        command.CommandTimeout = 15;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        logger.LogInformation(
            "Azure SQL connectivity verified for {DatabaseName} (SELECT 1 = {Result}, ServerVersion = {ServerVersion}, ManagedIdentity={ManagedIdentity})",
            databaseName,
            result,
            connection.ServerVersion,
            preferMi);
    }

    /// <summary>
    /// When Managed Identity is preferred, sets Authentication=Active Directory Default
    /// (uses DefaultAzureCredential under the hood) and clears password-based fields.
    /// </summary>
    public static string ApplyManagedIdentityIfNeeded(string connectionString, bool preferManagedIdentity)
    {
        if (!preferManagedIdentity)
        {
            return connectionString;
        }

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            Authentication = SqlAuthenticationMethod.ActiveDirectoryDefault,
            UserID = string.Empty,
            Password = string.Empty
        };

        if (!connectionString.Contains("Encrypt=", StringComparison.OrdinalIgnoreCase))
        {
            builder.Encrypt = true;
        }

        return builder.ConnectionString;
    }

    private static bool ResolvePreferManagedIdentity(SqlOptions sqlOptions, IConfiguration configuration)
    {
        if (sqlOptions.UseManagedIdentity.HasValue)
        {
            return sqlOptions.UseManagedIdentity.Value;
        }

        var azureAuth = configuration.GetSection(AzureAuthOptions.SectionName).Get<AzureAuthOptions>();
        if (azureAuth?.PreferManagedIdentity.HasValue == true)
        {
            return azureAuth.PreferManagedIdentity.Value;
        }

        var env = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        return string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
    }
}
