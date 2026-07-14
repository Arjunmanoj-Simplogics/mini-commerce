using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace MiniCommerce.BuildingBlocks.Auth;

public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Registers JWT bearer authentication using shared Jwt options.
    /// Signing key must come from environment / Key Vault in non-Development (no localhost/dev bleed).
    /// </summary>
    public static IServiceCollection AddMiniCommerceJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var env = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Production;

        if (!string.Equals(env, Environments.Development, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(jwt.SigningKey)
                || jwt.SigningKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
                || jwt.SigningKey.Contains("Dev_Signing_Key", StringComparison.OrdinalIgnoreCase)
                || jwt.SigningKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:SigningKey must be set via environment variable (Jwt__SigningKey) or Key Vault " +
                    "in non-Development environments (min 32 characters; no development placeholder).");
            }
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        return services;
    }
}
