using FluentValidation;
using InventoryService.Application.DTOs;
using InventoryService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCommerce.BuildingBlocks.Auth;
using MiniCommerce.Contracts.Inventory;

namespace InventoryService.API.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;
    private readonly IValidator<CreateInventoryItemDto> _createValidator;
    private readonly IValidator<UpdateInventoryItemDto> _updateValidator;

    public InventoryController(
        IInventoryService service,
        IValidator<CreateInventoryItemDto> createValidator,
        IValidator<UpdateInventoryItemDto> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
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
    /// </summary>
    [HttpPost("reserve")]
    public async Task<ActionResult<ReserveStockResponse>> Reserve([FromBody] ReserveStockRequest request, CancellationToken cancellationToken)
        => Ok(await _service.ReserveAsync(request, cancellationToken));

    /// <summary>
    /// Releases reserved stock (called by Order Service on cancel/delete).
    /// </summary>
    [HttpPost("release")]
    public async Task<IActionResult> Release([FromBody] ReleaseStockRequest request, CancellationToken cancellationToken)
    {
        await _service.ReleaseAsync(request, cancellationToken);
        return NoContent();
    }
}
