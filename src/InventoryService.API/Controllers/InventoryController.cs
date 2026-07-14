using FluentValidation;
using InventoryService.Application.DTOs;
using InventoryService.Application.Exceptions;
using InventoryService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCommerce.BuildingBlocks.Auth;
using MiniCommerce.Contracts.Events;
using MiniCommerce.Contracts.Inventory;
using MiniCommerce.Contracts.Messaging;
using MiniCommerce.Messaging.Abstractions;

namespace InventoryService.API.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;
    private readonly IValidator<CreateInventoryItemDto> _createValidator;
    private readonly IValidator<UpdateInventoryItemDto> _updateValidator;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryService service,
        IValidator<CreateInventoryItemDto> createValidator,
        IValidator<UpdateInventoryItemDto> updateValidator,
        IMessagePublisher messagePublisher,
        ILogger<InventoryController> logger)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryItemDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    [HttpGet("sku/{sku}")]
    public async Task<ActionResult<InventoryItemDto>> GetBySku(string sku, CancellationToken cancellationToken)
        => Ok(await _service.GetBySkuAsync(sku, cancellationToken));

    [HttpPost]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<InventoryItemDto>> Create([FromBody] CreateInventoryItemDto dto, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);
        var created = await _service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<ActionResult<InventoryItemDto>> Update(Guid id, [FromBody] UpdateInventoryItemDto dto, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(dto, cancellationToken);
        return Ok(await _service.UpdateAsync(id, dto, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AuthRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reserves stock for an order (called by Order Service).
    /// Publishes InventoryReserved or InventoryFailed integration events (when Service Bus is enabled).
    /// </summary>
    [HttpPost("reserve")]
    public async Task<ActionResult<ReserveStockResponse>> Reserve([FromBody] ReserveStockRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.ReserveAsync(request, cancellationToken);
            await PublishSafeAsync(
                ServiceBusNames.InventoryReserved,
                new InventoryReservedEvent
                {
                    OrderId = request.OrderId,
                    ProductSku = request.ProductSku,
                    Quantity = request.Quantity,
                    RemainingQuantity = result.RemainingQuantity
                },
                request.OrderId.ToString("N"),
                cancellationToken);
            return Ok(result);
        }
        catch (InsufficientStockException ex)
        {
            await PublishSafeAsync(
                ServiceBusNames.InventoryFailed,
                new InventoryFailedEvent
                {
                    OrderId = request.OrderId,
                    ProductSku = request.ProductSku,
                    Quantity = request.Quantity,
                    Reason = ex.Message
                },
                request.OrderId.ToString("N"),
                cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Releases reserved stock (called by Order Service on cancel/delete).
    /// </summary>
    [HttpPost("release")]
    public async Task<IActionResult> Release([FromBody] ReleaseStockRequest request, CancellationToken cancellationToken)
    {
        await _service.ReleaseAsync(request, cancellationToken);
        return NoContent();
    }

    private async Task PublishSafeAsync<T>(string eventType, T payload, string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            await _messagePublisher.PublishAsync(eventType, payload, correlationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} CorrelationId={CorrelationId}", eventType, correlationId);
        }
    }
}
