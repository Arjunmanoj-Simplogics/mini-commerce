using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniCommerce.Contracts.Events;
using MiniCommerce.Contracts.Messaging;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Integration;

/// <summary>
/// Publishes order integration events to an Azure Service Bus topic.
/// </summary>
public sealed class ServiceBusIntegrationEventPublisher : IIntegrationEventPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ServiceBusSender _sender;
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusIntegrationEventPublisher> _logger;

    public ServiceBusIntegrationEventPublisher(
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusIntegrationEventPublisher> logger)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException("ServiceBus:ConnectionString is required when ServiceBus is enabled.");
        }

        _client = new ServiceBusClient(settings.ConnectionString);
        _sender = _client.CreateSender(settings.TopicName);
        _logger = logger;
    }

    public Task PublishOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken cancellationToken = default)
        => PublishAsync(ServiceBusNames.OrderCreated, evt, cancellationToken);

    public Task PublishOrderStatusChangedAsync(OrderStatusChangedEvent evt, CancellationToken cancellationToken = default)
        => PublishAsync(ServiceBusNames.OrderStatusChanged, evt, cancellationToken);

    public Task PublishOrderCancelledAsync(OrderCancelledEvent evt, CancellationToken cancellationToken = default)
        => PublishAsync(ServiceBusNames.OrderCancelled, evt, cancellationToken);

    private async Task PublishAsync<T>(string eventType, T payload, CancellationToken cancellationToken)
    {
        try
        {
            var body = JsonSerializer.Serialize(payload, JsonOptions);
            var message = new ServiceBusMessage(body)
            {
                Subject = eventType,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString("N")
            };
            message.ApplicationProperties[ServiceBusNames.EventTypeProperty] = eventType;

            await _sender.SendMessageAsync(message, cancellationToken);
            _logger.LogInformation("Published {EventType} to Service Bus topic", eventType);
        }
        catch (Exception ex)
        {
            // Do not fail the order transaction if messaging is unavailable
            _logger.LogError(ex, "Failed to publish {EventType} to Service Bus", eventType);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
