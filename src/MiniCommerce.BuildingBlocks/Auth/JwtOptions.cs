namespace MiniCommerce.BuildingBlocks.Auth;

/// <summary>
/// Shared JWT configuration for all Mini Commerce APIs.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "MiniCommerce";
    public string Audience { get; set; } = "MiniCommerce";
    public string SigningKey { get; set; } = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_KEY_32+";
    public int ExpirationMinutes { get; set; } = 120;
}

public static class AuthRoles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
}
