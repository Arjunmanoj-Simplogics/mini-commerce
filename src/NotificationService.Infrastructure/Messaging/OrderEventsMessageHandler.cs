using System.Text.Json;
using Microsoft.Extensions.Logging;
using MiniCommerce.Contracts.Events;
using MiniCommerce.Contracts.Messaging;
using MiniCommerce.Messaging.Abstractions;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Dispatches OrderCreated / OrderStatusChanged / OrderCancelled messages to the notification application service.
/// </summary>
public sealed class OrderEventsMessageHandler : IMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] Supported =
    [
        ServiceBusNames.OrderCreated,
        ServiceBusNames.OrderStatusChanged,
        ServiceBusNames.OrderCancelled
    ];

    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderEventsMessageHandler> _logger;

    public OrderEventsMessageHandler(
        INotificationService notificationService,
        ILogger<OrderEventsMessageHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> EventTypes => Supported;

    /// <inheritdoc />
    public async Task HandleAsync(string eventType, string body, string? correlationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling {EventType} CorrelationId={CorrelationId}",
            eventType,
            correlationId ?? "(none)");

        switch (eventType)
        {
            case ServiceBusNames.OrderCreated:
                var created = JsonSerializer.Deserialize<OrderCreatedEvent>(body, JsonOptions)
                    ?? throw new InvalidOperationException("Invalid OrderCreatedEvent payload.");
                await _notificationService.HandleOrderCreatedAsync(created, cancellationToken);
                break;

            case ServiceBusNames.OrderStatusChanged:
                var statusChanged = JsonSerializer.Deserialize<OrderStatusChangedEvent>(body, JsonOptions)
                    ?? throw new InvalidOperationException("Invalid OrderStatusChangedEvent payload.");
                await _notificationService.HandleOrderStatusChangedAsync(statusChanged, cancellationToken);
                break;

            case ServiceBusNames.OrderCancelled:
                var cancelled = JsonSerializer.Deserialize<OrderCancelledEvent>(body, JsonOptions)
                    ?? throw new InvalidOperationException("Invalid OrderCancelledEvent payload.");
                await _notificationService.HandleOrderCancelledAsync(cancelled, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unsupported event type '{eventType}'.");
        }
    }
}
