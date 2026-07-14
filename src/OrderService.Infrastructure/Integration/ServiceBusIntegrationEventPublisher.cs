using Microsoft.Extensions.Logging;
using MiniCommerce.Contracts.Events;
using MiniCommerce.Contracts.Messaging;
using MiniCommerce.Messaging.Abstractions;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Integration;

/// <summary>
/// Publishes order integration events via <see cref="IMessagePublisher"/> (Service Bus when enabled).
/// Failures are logged and do not fail the order business flow.
/// </summary>
public sealed class ServiceBusIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<ServiceBusIntegrationEventPublisher> _logger;

    public ServiceBusIntegrationEventPublisher(
        IMessagePublisher publisher,
        ILogger<ServiceBusIntegrationEventPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public Task PublishOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken cancellationToken = default)
        => PublishSafeAsync(ServiceBusNames.OrderCreated, evt, evt.OrderId.ToString("N"), cancellationToken);

    public Task PublishOrderStatusChangedAsync(OrderStatusChangedEvent evt, CancellationToken cancellationToken = default)
        => PublishSafeAsync(ServiceBusNames.OrderStatusChanged, evt, evt.OrderId.ToString("N"), cancellationToken);

    public Task PublishOrderCancelledAsync(OrderCancelledEvent evt, CancellationToken cancellationToken = default)
        => PublishSafeAsync(ServiceBusNames.OrderCancelled, evt, evt.OrderId.ToString("N"), cancellationToken);

    private async Task PublishSafeAsync<T>(string eventType, T payload, string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            await _publisher.PublishAsync(eventType, payload, correlationId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Mirror previous behaviour: messaging failures must not break order APIs
            _logger.LogError(ex, "Failed to publish {EventType} CorrelationId={CorrelationId}", eventType, correlationId);
        }
    }
}
