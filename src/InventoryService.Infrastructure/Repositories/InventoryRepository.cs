using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryService.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<InventoryRepository> _logger;

    public InventoryRepository(InventoryDbContext context, ILogger<InventoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.InventoryItems.AsNoTracking().OrderBy(x => x.ProductSku).ToListAsync(cancellationToken);

    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.InventoryItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<InventoryItem?> GetBySkuAsync(string productSku, CancellationToken cancellationToken = default)
        => await _context.InventoryItems.FirstOrDefaultAsync(
            x => x.ProductSku == productSku.ToUpperInvariant(), cancellationToken);

    public async Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        _context.InventoryItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.InventoryItems.FindAsync([id], cancellationToken);
        if (item is null) return false;
        _context.InventoryItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
