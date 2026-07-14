using Microsoft.Extensions.Logging;
using MiniCommerce.Messaging.Abstractions;

namespace MiniCommerce.Messaging;

/// <summary>
/// No-op publisher used when Service Bus is disabled (local HTTP fallback path).
/// </summary>
public sealed class NoOpMessagePublisher : IMessagePublisher
{
    private readonly ILogger<NoOpMessagePublisher> _logger;

    /// <summary>Initializes the no-op publisher.</summary>
    public NoOpMessagePublisher(ILogger<NoOpMessagePublisher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(
        string eventType,
        T payload,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Service Bus disabled; skipped publish of {EventType} CorrelationId={CorrelationId}",
            eventType,
            correlationId ?? "(none)");
        return Task.CompletedTask;
    }
}
