namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Cross-origin resource sharing (CORS) for the React storefront and other frontends.
/// Bound from the "Cors" configuration section.
/// Set via environment variables in Kubernetes, e.g. Cors__AllowedOrigins__0=https://shop.example.com.
/// </summary>
public class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Policy name registered by AddMiniCommerceCors. Do not change without updating all services.
    /// </summary>
    public const string FrontendPolicyName = "FrontendPolicy";

    /// <summary>
    /// Allowed browser origins. Empty in Production until set by env / ConfigMap
    /// (no baked-in localhost). Development falls back to Vite when unset.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
