using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniCommerce.AzureAuth;
using MiniCommerce.BuildingBlocks.Auth;
using MiniCommerce.BuildingBlocks.Configuration;
using MiniCommerce.BuildingBlocks.Data;
using MiniCommerce.BuildingBlocks.Health;
using MiniCommerce.BuildingBlocks.Hosting;
using MiniCommerce.BuildingBlocks.Logging;
using MiniCommerce.BuildingBlocks.Observability;
using Serilog;

namespace CartService.API;

public class Cart
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime UpdatedDate { get; set; }
    public List<CartItem> Items { get; set; } = [];
}

public class CartItem
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Cart? Cart { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options) { }
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(e =>
        {
            e.ToTable("Carts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasMany(x => x.Items).WithOne(i => i.Cart!).HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CartItem>(e =>
        {
            e.ToTable("CartItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductSku).HasMaxLength(50).IsRequired();
            e.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
        });
    }
}

public record CartItemDto(Guid Id, string ProductSku, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);
public record CartDto(Guid Id, Guid UserId, IReadOnlyList<CartItemDto> Items, decimal TotalAmount);
public record AddCartItemRequest(string ProductSku, string ProductName, decimal UnitPrice, int Quantity);
public record UpdateCartItemRequest(int Quantity);

[ApiController]
[Authorize]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly CartDbContext _db;

    public CartController(CartDbContext db) => _db = db;

    private Guid GetUserId()
    {
        var value =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("User id claim is missing from the token.");
        }

        return userId;
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid userId, CancellationToken ct)
    {
        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart { Id = Guid.NewGuid(), UserId = userId, UpdatedDate = DateTime.UtcNow };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(ct);
        return cart;
    }

    private async Task<CartDto> LoadCartDtoAsync(Guid cartId, CancellationToken ct)
    {
        var cart = await _db.Carts.AsNoTracking().FirstAsync(c => c.Id == cartId, ct);
        var items = await _db.CartItems.AsNoTracking()
            .Where(i => i.CartId == cartId)
            .OrderBy(i => i.ProductName)
            .ToListAsync(ct);

        return new CartDto(
            cart.Id,
            cart.UserId,
            items.Select(i => new CartItemDto(i.Id, i.ProductSku, i.ProductName, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity)).ToList(),
            items.Sum(i => i.UnitPrice * i.Quantity));
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken ct)
    {
        var cart = await GetOrCreateCartAsync(GetUserId(), ct);
        return Ok(await LoadCartDtoAsync(cart.Id, ct));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest(new { title = "Quantity must be greater than zero." });
        }

        var cart = await GetOrCreateCartAsync(GetUserId(), ct);
        var sku = request.ProductSku.Trim().ToUpperInvariant();

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductSku == sku, ct);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
            existing.UnitPrice = request.UnitPrice;
            existing.ProductName = request.ProductName.Trim();
        }
        else
        {
            _db.CartItems.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductSku = sku,
                ProductName = request.ProductName.Trim(),
                UnitPrice = request.UnitPrice,
                Quantity = request.Quantity
            });
        }

        cart.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(await LoadCartDtoAsync(cart.Id, ct));
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<ActionResult<CartDto>> UpdateItem(Guid itemId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        var cart = await GetOrCreateCartAsync(GetUserId(), ct);
        var item = await _db.CartItems.FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cart.Id, ct);
        if (item is null)
        {
            return NotFound();
        }

        if (request.Quantity <= 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
        }

        cart.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(await LoadCartDtoAsync(cart.Id, ct));
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid itemId, CancellationToken ct)
    {
        var cart = await GetOrCreateCartAsync(GetUserId(), ct);
        var item = await _db.CartItems.FirstOrDefaultAsync(i => i.Id == itemId && i.CartId == cart.Id, ct);
        if (item is null)
        {
            return NotFound();
        }

        _db.CartItems.Remove(item);
        cart.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(await LoadCartDtoAsync(cart.Id, ct));
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        var cart = await GetOrCreateCartAsync(GetUserId(), ct);
        var items = await _db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync(ct);
        _db.CartItems.RemoveRange(items);
        cart.UpdatedDate = DateTime.UtcNow;
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
            builder.Host.UseSerilog((_, _, c) => c
                .WriteTo.Console()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", "CartService"));

            builder.Services.AddMiniCommerceAzureCredential(builder.Configuration);
            builder.AddKeyVaultConfiguration();
            builder.AddMiniCommerceAksHosting();
            builder.Services.AddMiniCommerceOptions(builder.Configuration);
            builder.Services.AddMiniCommerceTelemetry(builder.Configuration);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMiniCommerceJwtAuthentication(builder.Configuration);

            var cs = builder.Configuration.GetRequiredSqlConnectionString(ConnectionStringNames.CartDB);
            builder.Services.AddDbContext<CartDbContext>(o =>
                o.UseAzureSqlServer(cs, builder.Configuration));
            builder.Services.AddMiniCommerceHealthChecks(builder.Configuration, cs);
            builder.Services.AddMiniCommerceCors(builder.Configuration);

            var app = builder.Build();
            app.UseMiniCommerceForwardedHeaders();
            app.UseMiniCommerceHttpsRedirection();
            app.UseMiniCommerceStructuredLogging();
            if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
            app.UseCors(CorsOptions.FrontendPolicyName);
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapMiniCommerceHealthEndpoints();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CartService.Sql");
                await AzureSqlExtensions.VerifySqlConnectivityAsync(cs, logger, "CartDB");
                await db.Database.EnsureCreatedAsync();
            }

            app.Run();
        }
        catch (Exception ex) { Log.Fatal(ex, "Cart Service terminated"); }
        finally { await Log.CloseAndFlushAsync(); }
    }
}
