namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Named SQL connection strings. Bound from the standard "ConnectionStrings" section.
/// Override via environment variables, e.g. ConnectionStrings__OrderDB=Server=...
/// </summary>
public class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>Auth service database. Env: ConnectionStrings__AuthDB</summary>
    public string? AuthDB { get; set; }

    /// <summary>Cart service database. Env: ConnectionStrings__CartDB</summary>
    public string? CartDB { get; set; }

    /// <summary>Catalog service database. Env: ConnectionStrings__CatalogDB</summary>
    public string? CatalogDB { get; set; }

    /// <summary>Order service database. Env: ConnectionStrings__OrderDB</summary>
    public string? OrderDB { get; set; }

    /// <summary>Inventory service database. Env: ConnectionStrings__InventoryDB</summary>
    public string? InventoryDB { get; set; }

    /// <summary>Notification service database. Env: ConnectionStrings__NotificationDB</summary>
    public string? NotificationDB { get; set; }

    /// <summary>
    /// Resolves a connection string by logical name (e.g. "OrderDB").
    /// </summary>
    public string? GetConnectionString(string name) =>
        name switch
        {
            ConnectionStringNames.AuthDB => AuthDB,
            ConnectionStringNames.CartDB => CartDB,
            ConnectionStringNames.CatalogDB => CatalogDB,
            ConnectionStringNames.OrderDB => OrderDB,
            ConnectionStringNames.InventoryDB => InventoryDB,
            ConnectionStringNames.NotificationDB => NotificationDB,
            _ => null
        };

    /// <summary>
    /// Returns the connection string or throws when it is missing or whitespace.
    /// </summary>
    public string GetRequiredConnectionString(string name) =>
        GetConnectionString(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"Connection string '{name}' is not configured. " +
                $"Set ConnectionStrings:{name} or environment variable ConnectionStrings__{name}.");
}

/// <summary>
/// Canonical connection string names used across Mini Commerce services.
/// </summary>
public static class ConnectionStringNames
{
    public const string AuthDB = "AuthDB";
    public const string CartDB = "CartDB";
    public const string CatalogDB = "CatalogDB";
    public const string OrderDB = "OrderDB";
    public const string InventoryDB = "InventoryDB";
    public const string NotificationDB = "NotificationDB";
}
