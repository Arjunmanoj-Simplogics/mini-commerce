using Microsoft.AspNetCore.Mvc;
using MiniCommerce.Contracts.Events;
using NotificationService.Application.Interfaces;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));
}

/// <summary>
/// Event ingestion endpoints (HTTP stand-in for Azure Service Bus consumers).
/// </summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly INotificationService _service;
    private readonly ILogger<EventsController> _logger;

    public EventsController(INotificationService service, ILogger<EventsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("order-created")]
    public async Task<IActionResult> OrderCreated([FromBody] OrderCreatedEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received OrderCreatedEvent for {OrderNumber}", evt.OrderNumber);
        await _service.HandleOrderCreatedAsync(evt, cancellationToken);
        return Accepted();
    }

    [HttpPost("order-status-changed")]
    public async Task<IActionResult> OrderStatusChanged([FromBody] OrderStatusChangedEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received OrderStatusChangedEvent for {OrderNumber}", evt.OrderNumber);
        await _service.HandleOrderStatusChangedAsync(evt, cancellationToken);
        return Accepted();
    }

    [HttpPost("order-cancelled")]
    public async Task<IActionResult> OrderCancelled([FromBody] OrderCancelledEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received OrderCancelledEvent for {OrderNumber}", evt.OrderNumber);
        await _service.HandleOrderCancelledAsync(evt, cancellationToken);
        return Accepted();
    }
}
