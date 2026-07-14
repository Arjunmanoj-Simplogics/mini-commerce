using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniCommerce.AzureAuth;
using MiniCommerce.Contracts.Messaging;
using MiniCommerce.Messaging.Abstractions;
using MiniCommerce.Messaging.Internal;
using MiniCommerce.Messaging.Options;

namespace MiniCommerce.Messaging;

/// <summary>
/// Publishes integration events to an Azure Service Bus topic.
/// Development: connection string. Production: Managed Identity via shared IAzureCredentialProvider.
/// </summary>
public sealed class ServiceBusPublisher : IMessagePublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusPublisher> _logger;

    /// <summary>
    /// Creates a publisher using the shared Azure credential provider.
    /// </summary>
    public ServiceBusPublisher(
        IOptions<ServiceBusOptions> options,
        IAzureCredentialProvider credentialProvider,
        ILogger<ServiceBusPublisher> logger)
        : this(options, new ServiceBusClientFactory(credentialProvider), logger)
    {
    }

    /// <summary>
    /// Testable constructor accepting a client factory.
    /// </summary>
    internal ServiceBusPublisher(
        IOptions<ServiceBusOptions> options,
        ServiceBusClientFactory clientFactory,
        ILogger<ServiceBusPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        var settings = options.Value;
        if (!settings.Enabled)
        {
            throw new InvalidOperationException(
                "ServiceBusPublisher was resolved but ServiceBus:Enabled is false.");
        }

        if (!clientFactory.CanCreate(settings))
        {
            throw new InvalidOperationException(
                "ServiceBus ConnectionString (Development) or FullyQualifiedNamespace (Production MI) is required when ServiceBus is enabled.");
        }

        _logger = logger;
        _client = clientFactory.Create(settings);
        _sender = _client.CreateSender(settings.TopicName);
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(
        string eventType,
        T payload,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(payload);

        var resolvedCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N")
            : correlationId;

        using var activity = MessagingTelemetry.ActivitySource.StartActivity(
            "ServiceBus.Publish",
            ActivityKind.Producer);
        activity?.SetTag("messaging.system", "servicebus");
        activity?.SetTag("messaging.operation", "publish");
        activity?.SetTag("messaging.destination.name", _sender.EntityPath);
        activity?.SetTag("messaging.message.type", eventType);
        activity?.SetTag("correlation.id", resolvedCorrelationId);

        try
        {
            var body = JsonSerializer.Serialize(payload, JsonOptions);
            var message = new ServiceBusMessage(body)
            {
                Subject = eventType,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString("N"),
                CorrelationId = resolvedCorrelationId
            };
            message.ApplicationProperties[ServiceBusNames.EventTypeProperty] = eventType;
            message.ApplicationProperties["CorrelationId"] = resolvedCorrelationId;

            // W3C Trace Context for cross-service linking
            if (activity is not null)
            {
                message.ApplicationProperties["Diagnostic-Id"] = activity.Id!;
                message.ApplicationProperties["TraceParent"] = activity.Id!;
            }

            await _sender.SendMessageAsync(message, cancellationToken);

            activity?.SetTag("messaging.message.id", message.MessageId);
            _logger.LogInformation(
                "Published {EventType} MessageId={MessageId} CorrelationId={CorrelationId} TraceId={TraceId} SpanId={SpanId}",
                eventType,
                message.MessageId,
                resolvedCorrelationId,
                activity?.TraceId.ToString(),
                activity?.SpanId.ToString());
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            _logger.LogError(
                ex,
                "Failed to publish {EventType} CorrelationId={CorrelationId} Exception={ExceptionType}",
                eventType,
                resolvedCorrelationId,
                ex.GetType().Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
