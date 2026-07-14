using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Registers the shared FrontendPolicy CORS policy from CorsOptions.
/// Origins must be supplied via configuration / environment variables in non-Development.
/// </summary>
public static class CorsServiceCollectionExtensions
{
    /// <summary>
    /// Binds CorsOptions and adds the FrontendPolicy CORS policy used by all APIs.
    /// </summary>
    public static IServiceCollection AddMiniCommerceCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));

        var cors = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
        var env = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Production;

        var isDevelopment = string.Equals(env, Environments.Development, StringComparison.OrdinalIgnoreCase);

        string[] origins;
        if (cors.AllowedOrigins is { Length: > 0 })
        {
            origins = cors.AllowedOrigins;
        }
        else if (isDevelopment)
        {
            // Local Vite only when neither appsettings nor env set Cors__AllowedOrigins__*
            origins = ["http://localhost:5173"];
        }
        else
        {
            // Production / Staging: require explicit origins (ConfigMap / env). Empty = deny all browsers.
            origins = [];
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.FrontendPolicyName, policy =>
            {
                if (origins.Length == 0)
                {
                    policy.SetIsOriginAllowed(_ => false)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    return;
                }

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
