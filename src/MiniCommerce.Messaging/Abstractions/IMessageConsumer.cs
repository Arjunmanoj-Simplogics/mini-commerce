namespace MiniCommerce.Messaging.Abstractions;

/// <summary>
/// Handles a deserialised Service Bus message. Implemented by application services;
/// invoked by <see cref="IMessageConsumer"/> / <c>ServiceBusConsumer</c>.
/// </summary>
public interface IMessageHandler
{
    /// <summary>Event type wire names this handler supports (e.g. OrderCreated).</summary>
    IReadOnlyCollection<string> EventTypes { get; }

    /// <summary>
    /// Processes the message body for a matching event type.
    /// Throw to trigger dead-letter handling.
    /// </summary>
    Task HandleAsync(string eventType, string body, string? correlationId, CancellationToken cancellationToken);
}

/// <summary>
/// Consumes messages from a Service Bus subscription (typically via BackgroundService).
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Starts consumption until <paramref name="cancellationToken"/> is cancelled.
    /// No-op when Service Bus is disabled or not configured.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>Stops the underlying processor gracefully.</summary>
    Task StopAsync(CancellationToken cancellationToken);
}
