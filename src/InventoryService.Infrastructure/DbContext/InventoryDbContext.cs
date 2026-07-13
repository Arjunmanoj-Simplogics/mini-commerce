using InventoryService.Domain.Constants;
using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.DbContext;

public class InventoryDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InventoryItemConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductSku).IsRequired().HasMaxLength(InventoryConstants.MaxSkuLength);
        builder.HasIndex(x => x.ProductSku).IsUnique();
        builder.Property(x => x.ProductName).IsRequired().HasMaxLength(InventoryConstants.MaxProductNameLength);
    }
}
