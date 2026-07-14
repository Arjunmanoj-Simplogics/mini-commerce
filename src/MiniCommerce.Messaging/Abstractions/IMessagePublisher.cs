namespace MiniCommerce.Messaging.Abstractions;

/// <summary>
/// Application-facing message publisher. Controllers and services depend on this
/// interface — never on the Azure Service Bus SDK.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a typed integration event to the configured topic.
    /// </summary>
    /// <typeparam name="T">Event payload type.</typeparam>
    /// <param name="eventType">Wire event type name (e.g. <c>OrderCreated</c>).</param>
    /// <param name="payload">JSON-serializable event body.</param>
    /// <param name="correlationId">Optional correlation id (defaults to a new GUID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(
        string eventType,
        T payload,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
