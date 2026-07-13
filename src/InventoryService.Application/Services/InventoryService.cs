using AutoMapper;
using InventoryService.Application.DTOs;
using InventoryService.Application.Exceptions;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using Microsoft.Extensions.Logging;
using MiniCommerce.Contracts.Inventory;

namespace InventoryService.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(IInventoryRepository repository, IMapper mapper, ILogger<InventoryService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<InventoryItemDto>>(items);
    }

    public async Task<InventoryItemDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Inventory item '{id}' was not found.");
        return _mapper.Map<InventoryItemDto>(item);
    }

    public async Task<InventoryItemDto> GetBySkuAsync(string productSku, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetBySkuAsync(productSku, cancellationToken)
            ?? throw new NotFoundException($"Inventory item with SKU '{productSku}' was not found.");
        return _mapper.Map<InventoryItemDto>(item);
    }

    public async Task<InventoryItemDto> CreateAsync(CreateInventoryItemDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetBySkuAsync(dto.ProductSku.Trim(), cancellationToken);
        if (existing is not null)
        {
            throw new ValidationException($"Product SKU '{dto.ProductSku}' already exists.");
        }

        var utcNow = DateTime.UtcNow;
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductSku = dto.ProductSku.Trim().ToUpperInvariant(),
            ProductName = dto.ProductName.Trim(),
            QuantityAvailable = dto.QuantityAvailable,
            QuantityReserved = 0,
            CreatedDate = utcNow,
            UpdatedDate = utcNow
        };

        var created = await _repository.CreateAsync(item, cancellationToken);
        _logger.LogInformation("Inventory item {Sku} created", created.ProductSku);
        return _mapper.Map<InventoryItemDto>(created);
    }

    public async Task<InventoryItemDto> UpdateAsync(Guid id, UpdateInventoryItemDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Inventory item '{id}' was not found.");

        item.ProductName = dto.ProductName.Trim();
        item.QuantityAvailable = dto.QuantityAvailable;
        item.UpdatedDate = DateTime.UtcNow;

        await _repository.UpdateAsync(item, cancellationToken);
        return _mapper.Map<InventoryItemDto>(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundException($"Inventory item '{id}' was not found.");
        }
    }

    public async Task<ReserveStockResponse> ReserveAsync(ReserveStockRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetBySkuAsync(request.ProductSku.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException($"Inventory item with SKU '{request.ProductSku}' was not found.");

        if (item.QuantityAvailable < request.Quantity)
        {
            throw new InsufficientStockException(
                $"Insufficient stock for SKU '{request.ProductSku}'. Available: {item.QuantityAvailable}, requested: {request.Quantity}.");
        }

        item.QuantityAvailable -= request.Quantity;
        item.QuantityReserved += request.Quantity;
        item.UpdatedDate = DateTime.UtcNow;

        await _repository.UpdateAsync(item, cancellationToken);
        _logger.LogInformation(
            "Reserved {Quantity} of {Sku} for order {OrderId}. Remaining available: {Remaining}",
            request.Quantity, item.ProductSku, request.OrderId, item.QuantityAvailable);

        return new ReserveStockResponse
        {
            Success = true,
            Message = "Stock reserved successfully.",
            RemainingQuantity = item.QuantityAvailable
        };
    }

    public async Task ReleaseAsync(ReleaseStockRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetBySkuAsync(request.ProductSku.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException($"Inventory item with SKU '{request.ProductSku}' was not found.");

        var releaseQty = Math.Min(request.Quantity, item.QuantityReserved);
        item.QuantityReserved -= releaseQty;
        item.QuantityAvailable += releaseQty;
        item.UpdatedDate = DateTime.UtcNow;

        await _repository.UpdateAsync(item, cancellationToken);
        _logger.LogInformation(
            "Released {Quantity} of {Sku} for order {OrderId}",
            releaseQty, item.ProductSku, request.OrderId);
    }
}
