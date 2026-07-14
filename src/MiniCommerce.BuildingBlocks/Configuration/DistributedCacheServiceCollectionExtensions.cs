using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MiniCommerce.BuildingBlocks.Configuration;

/// <summary>
/// Registers distributed cache for multi-replica pods.
/// Uses Redis when <c>ConnectionStrings:Redis</c> / <c>Redis:ConnectionString</c> is set;
/// otherwise in-process <see cref="Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache"/> (single-pod / local).
/// </summary>
public static class DistributedCacheServiceCollectionExtensions
{
    /// <summary>
    /// Adds Redis or memory distributed cache based on configuration.
    /// </summary>
    public static IServiceCollection AddMiniCommerceDistributedCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var redis = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"]
            ?? configuration["ConnectionStrings:Redis"];

        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redis;
                options.InstanceName = configuration["Redis:InstanceName"] ?? "minicommerce:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
