using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniCommerce.Messaging.Abstractions;

namespace MiniCommerce.Messaging;

/// <summary>
/// Hosted <see cref="BackgroundService"/> that runs <see cref="IMessageConsumer"/>.
/// </summary>
public sealed class ServiceBusConsumerHostedService : BackgroundService
{
    private readonly IMessageConsumer _consumer;
    private readonly ILogger<ServiceBusConsumerHostedService> _logger;

    /// <summary>
    /// Creates the hosted wrapper around an <see cref="IMessageConsumer"/>.
    /// </summary>
    public ServiceBusConsumerHostedService(
        IMessageConsumer consumer,
        ILogger<ServiceBusConsumerHostedService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _consumer.StartAsync(stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service Bus consumer hosted service terminated unexpectedly");
            throw;
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumer.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
