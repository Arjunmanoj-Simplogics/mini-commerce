using InventoryService.Domain.Entities;

namespace InventoryService.Application.Interfaces;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetBySkuAsync(string productSku, CancellationToken cancellationToken = default);
    Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IInventoryService
{
    Task<IReadOnlyList<DTOs.InventoryItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DTOs.InventoryItemDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DTOs.InventoryItemDto> GetBySkuAsync(string productSku, CancellationToken cancellationToken = default);
    Task<DTOs.InventoryItemDto> CreateAsync(DTOs.CreateInventoryItemDto dto, CancellationToken cancellationToken = default);
    Task<DTOs.InventoryItemDto> UpdateAsync(Guid id, DTOs.UpdateInventoryItemDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MiniCommerce.Contracts.Inventory.ReserveStockResponse> ReserveAsync(MiniCommerce.Contracts.Inventory.ReserveStockRequest request, CancellationToken cancellationToken = default);
    Task ReleaseAsync(MiniCommerce.Contracts.Inventory.ReleaseStockRequest request, CancellationToken cancellationToken = default);
}
