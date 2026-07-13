using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Infrastructure.DbContext;

namespace OrderService.Infrastructure.Seed;

/// <summary>
/// Seeds initial order data when the database is empty.
/// </summary>
public static class OrderDbSeed
{
    /// <summary>
    /// Seeds sample orders if none exist.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedAsync(OrderDbContext context)
    {
        if (await context.Orders.AnyAsync())
        {
            return;
        }

        var utcNow = DateTime.UtcNow;

        var orders = new List<Order>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-20260101-SAMPLE01",
                CustomerName = "Jane Doe",
                Email = "jane.doe@example.com",
                ProductSku = "SKU-LAPTOP-01",
                Quantity = 1,
                TotalAmount = 149.99m,
                Status = OrderStatus.Processing,
                CreatedDate = utcNow.AddDays(-2),
                UpdatedDate = utcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-20260102-SAMPLE02",
                CustomerName = "John Smith",
                Email = "john.smith@example.com",
                ProductSku = "SKU-PHONE-01",
                Quantity = 2,
                TotalAmount = 89.50m,
                Status = OrderStatus.Pending,
                CreatedDate = utcNow.AddDays(-1),
                UpdatedDate = utcNow.AddDays(-1)
            }
        };

        await context.Orders.AddRangeAsync(orders);
        await context.SaveChangesAsync();
    }
}
