using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniCommerce.BuildingBlocks.Configuration;
using MiniCommerce.BuildingBlocks.Data;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.DbContext;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Seed;

namespace OrderService.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services including EF Core and repositories.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredSqlConnectionString(ConnectionStringNames.OrderDB);

        services.AddDbContext<OrderDbContext>(options =>
            options.UseAzureSqlServer(
                connectionString,
                configuration,
                migrationsAssembly: typeof(OrderDbContext).Assembly.FullName));

        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }

    /// <summary>
    /// Applies database migrations and seeds initial data.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OrderService.Sql");
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetRequiredSqlConnectionString(ConnectionStringNames.OrderDB);

        // Explicit Microsoft.Data.SqlClient probe before EF migrations
        await AzureSqlExtensions.VerifySqlConnectivityAsync(connectionString, logger, "OrderDB");

        await context.Database.MigrateAsync();
        await OrderDbSeed.SeedAsync(context);
    }
}