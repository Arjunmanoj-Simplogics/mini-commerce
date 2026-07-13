using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniCommerce.Contracts.Events;
using MiniCommerce.Contracts.Messaging;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Consumes order integration events from Azure Service Bus and creates notifications.
/// </summary>
public sealed class OrderEventsConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusOptions _options;
    private readonly ILogger<OrderEventsConsumer> _logger;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    public OrderEventsConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<ServiceBusOptions> options,
        ILogger<OrderEventsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Service Bus consumer is disabled. HTTP event endpoints remain available.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogWarning("ServiceBus:Enabled is true but ConnectionString is missing. Consumer will not start.");
            return;
        }

        _client = new ServiceBusClient(_options.ConnectionString);
        _processor = _client.CreateProcessor(_options.TopicName, _options.SubscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 4
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation(
            "Started Service Bus processor on topic {Topic} / subscription {Subscription}",
            _options.TopicName,
            _options.SubscriptionName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var eventType = args.Message.Subject;
        if (string.IsNullOrWhiteSpace(eventType) &&
            args.Message.ApplicationProperties.TryGetValue(ServiceBusNames.EventTypeProperty, out var prop) &&
            prop is string propValue)
        {
            eventType = propValue;
        }

        var body = Encoding.UTF8.GetString(args.Message.Body);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            switch (eventType)
            {
                case ServiceBusNames.OrderCreated:
                    var created = JsonSerializer.Deserialize<OrderCreatedEvent>(body, JsonOptions)
                        ?? throw new InvalidOperationException("Invalid OrderCreatedEvent payload.");
                    await notificationService.HandleOrderCreatedAsync(created, args.CancellationToken);
                    break;

                case ServiceBusNames.OrderStatusChanged:
                    var statusChanged = JsonSerializer.Deserialize<OrderStatusChangedEvent>(body, JsonOptions)
                        ?? throw new InvalidOperationException("Invalid OrderStatusChangedEvent payload.");
                    await notificationService.HandleOrderStatusChangedAsync(statusChanged, args.CancellationToken);
                    break;

                case ServiceBusNames.OrderCancelled:
                    var cancelled = JsonSerializer.Deserialize<OrderCancelledEvent>(body, JsonOptions)
                        ?? throw new InvalidOperationException("Invalid OrderCancelledEvent payload.");
                    await notificationService.HandleOrderCancelledAsync(cancelled, args.CancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown event type {EventType}; dead-lettering message", eventType);
                    await args.DeadLetterMessageAsync(args.Message, "UnknownEventType", $"Unknown event type: {eventType}");
                    return;
            }

            await args.CompleteMessageAsync(args.Message);
            _logger.LogInformation("Processed Service Bus event {EventType} ({MessageId})", eventType, args.Message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing Service Bus message {MessageId}", args.Message.MessageId);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processor error: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
