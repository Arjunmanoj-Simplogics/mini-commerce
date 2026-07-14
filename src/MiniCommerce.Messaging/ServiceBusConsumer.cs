using System.Diagnostics;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniCommerce.AzureAuth;
using MiniCommerce.Contracts.Messaging;
using MiniCommerce.Messaging.Abstractions;
using MiniCommerce.Messaging.Internal;
using MiniCommerce.Messaging.Options;

namespace MiniCommerce.Messaging;

/// <summary>
/// Consumes messages from an Azure Service Bus topic subscription.
/// Supports dead-letter on poison/unknown messages, correlation id propagation,
/// exponential backoff (SDK), and structured logging.
/// </summary>
public sealed class ServiceBusConsumer : IMessageConsumer, IAsyncDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusOptions _options;
    private readonly ServiceBusClientFactory _clientFactory;
    private readonly ILogger<ServiceBusConsumer> _logger;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    /// <summary>
    /// Creates a consumer. Handlers are resolved from DI per message (scoped).
    /// </summary>
    public ServiceBusConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<ServiceBusOptions> options,
        IAzureCredentialProvider credentialProvider,
        ILogger<ServiceBusConsumer> logger)
        : this(scopeFactory, options, new ServiceBusClientFactory(credentialProvider), logger)
    {
    }

    internal ServiceBusConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<ServiceBusOptions> options,
        ServiceBusClientFactory clientFactory,
        ILogger<ServiceBusConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Service Bus consumer is disabled. Skipping processor start.");
            return;
        }

        if (!_clientFactory.CanCreate(_options))
        {
            _logger.LogWarning(
                "ServiceBus:Enabled is true but ConnectionString/FullyQualifiedNamespace is missing. Consumer will not start.");
            return;
        }

        _client = _clientFactory.Create(_options);
        _processor = _client.CreateProcessor(
            _options.TopicName,
            _options.SubscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = Math.Max(1, _options.MaxConcurrentCalls),
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(Math.Max(1, _options.MaxAutoLockRenewalMinutes))
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken);
        _logger.LogInformation(
            "Started Service Bus processor on topic {Topic} / subscription {Subscription}",
            _options.TopicName,
            _options.SubscriptionName);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
            _processor = null;
        }

        if (_client is not null)
        {
            await _client.DisposeAsync();
            _client = null;
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var eventType = ResolveEventType(args.Message);
        var correlationId = ResolveCorrelationId(args.Message);
        var body = Encoding.UTF8.GetString(args.Message.Body);

        using var activity = StartConsumerActivity(args.Message, eventType, correlationId);

        _logger.LogInformation(
            "Received {EventType} MessageId={MessageId} CorrelationId={CorrelationId} DeliveryCount={DeliveryCount} TraceId={TraceId} SpanId={SpanId}",
            eventType,
            args.Message.MessageId,
            correlationId,
            args.Message.DeliveryCount,
            activity?.TraceId.ToString(),
            activity?.SpanId.ToString());

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IMessageHandler>().ToList();
            var handler = handlers.FirstOrDefault(h =>
                h.EventTypes.Contains(eventType, StringComparer.OrdinalIgnoreCase));

            if (handler is null)
            {
                _logger.LogWarning(
                    "Unknown event type {EventType}; dead-lettering MessageId={MessageId} CorrelationId={CorrelationId}",
                    eventType,
                    args.Message.MessageId,
                    correlationId);
                await args.DeadLetterMessageAsync(args.Message, "UnknownEventType", $"Unknown event type: {eventType}");
                activity?.SetStatus(ActivityStatusCode.Error, "UnknownEventType");
                return;
            }

            await handler.HandleAsync(eventType, body, correlationId, args.CancellationToken);
            await args.CompleteMessageAsync(args.Message);

            _logger.LogInformation(
                "Processed {EventType} MessageId={MessageId} CorrelationId={CorrelationId} TraceId={TraceId} SpanId={SpanId}",
                eventType,
                args.Message.MessageId,
                correlationId,
                activity?.TraceId.ToString(),
                activity?.SpanId.ToString());
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);

            _logger.LogError(
                ex,
                "Failed processing MessageId={MessageId} CorrelationId={CorrelationId} Exception={ExceptionType}; dead-lettering",
                args.Message.MessageId,
                correlationId,
                ex.GetType().Name);

            await args.DeadLetterMessageAsync(
                args.Message,
                "ProcessingFailed",
                Truncate(ex.Message, 4096));
        }
    }

    private static Activity? StartConsumerActivity(
        ServiceBusReceivedMessage message,
        string eventType,
        string correlationId)
    {
        ActivityContext parentContext = default;
        var hasParent = false;

        if (message.ApplicationProperties.TryGetValue("Diagnostic-Id", out var diagnosticId)
            && diagnosticId is string diagnostic
            && ActivityContext.TryParse(diagnostic, null, out parentContext))
        {
            hasParent = true;
        }
        else if (message.ApplicationProperties.TryGetValue("TraceParent", out var traceParent)
                 && traceParent is string tp
                 && ActivityContext.TryParse(tp, null, out parentContext))
        {
            hasParent = true;
        }

        var activity = hasParent
            ? MessagingTelemetry.ActivitySource.StartActivity(
                "ServiceBus.Process",
                ActivityKind.Consumer,
                parentContext)
            : MessagingTelemetry.ActivitySource.StartActivity(
                "ServiceBus.Process",
                ActivityKind.Consumer);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("messaging.system", "servicebus");
        activity.SetTag("messaging.operation", "process");
        activity.SetTag("messaging.message.type", eventType);
        activity.SetTag("messaging.message.id", message.MessageId);
        activity.SetTag("correlation.id", correlationId);
        return activity;
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus processor error ErrorSource={ErrorSource} Exception={ExceptionType}",
            args.ErrorSource,
            args.Exception.GetType().Name);
        return Task.CompletedTask;
    }

    private static string ResolveEventType(ServiceBusReceivedMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.Subject))
        {
            return message.Subject;
        }

        if (message.ApplicationProperties.TryGetValue(ServiceBusNames.EventTypeProperty, out var prop) &&
            prop is string propValue &&
            !string.IsNullOrWhiteSpace(propValue))
        {
            return propValue;
        }

        return string.Empty;
    }

    private static string ResolveCorrelationId(ServiceBusReceivedMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            return message.CorrelationId;
        }

        if (message.ApplicationProperties.TryGetValue("CorrelationId", out var prop) &&
            prop is string value &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return message.MessageId ?? Guid.NewGuid().ToString("N");
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None);
}
