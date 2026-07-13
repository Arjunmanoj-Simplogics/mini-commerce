using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCommerce.BuildingBlocks.Auth;
using Serilog;

namespace CatalogService.API;

public class Product
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.Sku).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Category).HasMaxLength(80).IsRequired();
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.Price).HasPrecision(18, 2);
        });
    }
}

public record ProductDto(Guid Id, string Sku, string Name, string Description, string Category, string ImageUrl, decimal Price, bool IsActive);
public record CreateProductRequest(string Sku, string Name, string Description, string Category, string ImageUrl, decimal Price);
public record UpdateProductRequest(string Name, string Description, string Category, string ImageUrl, decimal Price, bool IsActive);

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public CatalogController(CatalogDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.Description, p.Category, p.ImageUrl, p.Price, p.IsActive))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken ct)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? NotFound() : Ok(new ProductDto(p.Id, p.Sku, p.Name, p.Description, p.Category, p.ImageUrl, p.Price, p.IsActive));
    }

    [HttpGet("sku/{sku}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetBySku(string sku, CancellationToken ct)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Sku == sku.ToUpperInvariant(), ct);
        return p is null ? NotFound() : Ok(new ProductDto(p.Id, p.Sku, p.Name, p.Description, p.Category, p.ImageUrl, p.Price, p.IsActive));
    }

    [HttpPost]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(p => p.Sku == sku, ct))
        {
            return BadRequest(new { title = $"SKU '{sku}' already exists." });
        }

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim(),
            ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
            Price = request.Price,
            IsActive = true,
            CreatedDate = now,
            UpdatedDate = now
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            new ProductDto(product.Id, product.Sku, product.Name, product.Description, product.Category, product.ImageUrl, product.Price, product.IsActive));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return NotFound();

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Category = string.IsNullOrWhiteSpace(request.Category) ? product.Category : request.Category.Trim();
        product.ImageUrl = request.ImageUrl?.Trim() ?? product.ImageUrl;
        product.Price = request.Price;
        product.IsActive = request.IsActive;
        product.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(new ProductDto(product.Id, product.Sku, product.Name, product.Description, product.Category, product.ImageUrl, product.Price, product.IsActive));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return NotFound();
        product.IsActive = false;
        product.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((_, _, c) => c.WriteTo.Console());

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMiniCommerceJwtAuthentication(builder.Configuration);

            var cs = builder.Configuration.GetConnectionString("CatalogDB")
                ?? throw new InvalidOperationException("ConnectionStrings:CatalogDB is required.");
            builder.Services.AddDbContext<CatalogDbContext>(o => o.UseSqlServer(cs));
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
                .AddSqlServer(cs, name: "sqlserver", tags: ["ready"]);

            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
            builder.Services.AddCors(o => o.AddPolicy("FrontendPolicy", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

            var app = builder.Build();
            if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
            app.UseCors("FrontendPolicy");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/api/health");
            app.MapHealthChecks("/api/health/live", new() { Predicate = c => c.Tags.Contains("live") });
            app.MapHealthChecks("/api/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
                await db.Database.EnsureCreatedAsync();
                if (!await db.Products.AnyAsync())
                {
                    var now = DateTime.UtcNow;
                    db.Products.AddRange(
                        Seed("SKU-LAPTOP-01", "AeroBook 14", "Featherweight aluminum laptop for everyday work.", "Computers", 999.99m, "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800&q=80", now),
                        Seed("SKU-PHONE-01", "Nova Phone X", "Flagship camera phone with all-day battery.", "Phones", 699.00m, "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=800&q=80", now),
                        Seed("SKU-HEADSET-01", "QuietWave Headset", "Wireless ANC over-ear headphones.", "Audio", 149.50m, "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&q=80", now),
                        Seed("SKU-WATCH-01", "Pulse Watch Pro", "Fitness watch with bright always-on display.", "Wearables", 249.00m, "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&q=80", now),
                        Seed("SKU-TABLET-01", "Canvas Tab 11", "Portable tablet for streaming and sketching.", "Tablets", 449.00m, "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800&q=80", now),
                        Seed("SKU-SPEAKER-01", "RoomBeat Speaker", "Compact Bluetooth speaker with rich bass.", "Audio", 89.00m, "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=800&q=80", now),
                        Seed("SKU-KEYBOARD-01", "ClickForge Keyboard", "Mechanical keyboard with warm backlight.", "Accessories", 129.00m, "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800&q=80", now),
                        Seed("SKU-CAMERA-01", "Vista Mirrorless", "Travel-ready mirrorless camera kit.", "Cameras", 1199.00m, "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?w=800&q=80", now));
                    await db.SaveChangesAsync();
                }
            }

            app.Run();
        }
        catch (Exception ex) { Log.Fatal(ex, "Catalog Service terminated"); }
        finally { await Log.CloseAndFlushAsync(); }
    }

    private static Product Seed(string sku, string name, string description, string category, decimal price, string imageUrl, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Name = name,
            Description = description,
            Category = category,
            ImageUrl = imageUrl,
            Price = price,
            IsActive = true,
            CreatedDate = now,
            UpdatedDate = now
        };
}
