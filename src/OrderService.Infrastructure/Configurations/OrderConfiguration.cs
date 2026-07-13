using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Constants;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Order"/> entity.
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(OrderConstants.MaxOrderNumberLength);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(OrderConstants.MaxCustomerNameLength);

        builder.Property(o => o.Email)
            .IsRequired()
            .HasMaxLength(OrderConstants.MaxEmailLength);

        builder.Property(o => o.ProductSku)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.Quantity)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.CreatedDate)
            .IsRequired();

        builder.Property(o => o.UpdatedDate)
            .IsRequired();
    }
}
