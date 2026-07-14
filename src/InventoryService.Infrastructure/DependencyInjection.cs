using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.DbContext;
using InventoryService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniCommerce.BuildingBlocks.Configuration;
using MiniCommerce.BuildingBlocks.Data;

namespace InventoryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetRequiredSqlConnectionString(ConnectionStringNames.InventoryDB);

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseAzureSqlServer(
                connectionString,
                configuration,
                migrationsAssembly: typeof(InventoryDbContext).Assembly.FullName));

        services.AddScoped<IInventoryRepository, InventoryRepository>();
        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("InventoryService.Sql");
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetRequiredSqlConnectionString(ConnectionStringNames.InventoryDB);

        await AzureSqlExtensions.VerifySqlConnectivityAsync(connectionString, logger, "InventoryDB");

        await context.Database.EnsureCreatedAsync();

        if (!await context.InventoryItems.AnyAsync())
        {
            var utcNow = DateTime.UtcNow;
            context.InventoryItems.AddRange(
                Item("SKU-LAPTOP-01", "AeroBook 14", 25, utcNow),
                Item("SKU-PHONE-01", "Nova Phone X", 100, utcNow),
                Item("SKU-HEADSET-01", "QuietWave Headset", 50, utcNow),
                Item("SKU-WATCH-01", "Pulse Watch Pro", 40, utcNow),
                Item("SKU-TABLET-01", "Canvas Tab 11", 35, utcNow),
                Item("SKU-SPEAKER-01", "RoomBeat Speaker", 80, utcNow),
                Item("SKU-KEYBOARD-01", "ClickForge Keyboard", 60, utcNow),
                Item("SKU-CAMERA-01", "Vista Mirrorless", 15, utcNow));
            await context.SaveChangesAsync();
        }
    }

    private static InventoryItem Item(string sku, string name, int available, DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductSku = sku,
            ProductName = name,
            QuantityAvailable = available,
            QuantityReserved = 0,
            CreatedDate = utcNow,
            UpdatedDate = utcNow
        };
}
