using Microsoft.Extensions.Configuration;

namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Resolves SQL connection strings from IConfiguration (appsettings, env vars, Key Vault).
/// </summary>
public static class ConnectionStringConfigurationExtensions
{
    /// <summary>
    /// Reads a connection string using ASP.NET Core conventions (ConnectionStrings section + env vars).
    /// </summary>
    public static string? GetSqlConnectionString(this IConfiguration configuration, string name)
    {
        var fromStandard = configuration.GetConnectionString(name);
        if (!string.IsNullOrWhiteSpace(fromStandard))
        {
            return fromStandard;
        }

        return configuration.GetSection(ConnectionStringsOptions.SectionName)[name];
    }

    /// <summary>
    /// Returns the connection string or throws when it is missing or whitespace.
    /// </summary>
    public static string GetRequiredSqlConnectionString(this IConfiguration configuration, string name)
    {
        var value = configuration.GetSqlConnectionString(name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException(
            $"Connection string '{name}' is not configured. " +
            $"Set ConnectionStrings:{name} or environment variable ConnectionStrings__{name}.");
    }
}
