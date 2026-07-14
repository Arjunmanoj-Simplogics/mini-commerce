namespace MiniCommerce.BuildingBlocks.Auth;

/// <summary>
/// Shared JWT bearer token configuration for all Mini Commerce APIs.
/// Bound from the "Jwt" section. Override via env vars, e.g. Jwt__SigningKey=...
/// Never use default SigningKey in production — set via Key Vault or environment variables.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Token issuer claim. Env: Jwt__Issuer</summary>
    public string Issuer { get; set; } = "MiniCommerce";

    /// <summary>Token audience claim. Env: Jwt__Audience</summary>
    public string Audience { get; set; } = "MiniCommerce";

    /// <summary>Symmetric signing key (min 32 chars). Env: Jwt__SigningKey. No safe Production default.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access token lifetime in minutes. Env: Jwt__ExpirationMinutes</summary>
    public int ExpirationMinutes { get; set; } = 120;
}

public static class AuthRoles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
}
