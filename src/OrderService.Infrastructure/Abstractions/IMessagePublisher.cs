namespace OrderService.Infrastructure.Abstractions;

/// <summary>
/// Marker interface for future Azure Service Bus integration.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to a topic or queue.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default);
}
