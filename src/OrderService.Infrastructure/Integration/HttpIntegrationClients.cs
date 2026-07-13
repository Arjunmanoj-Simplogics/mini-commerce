using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using MiniCommerce.Contracts.Events;
using MiniCommerce.Contracts.Inventory;
using OrderService.Application.Exceptions;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Integration;

public class InventoryHttpClient : IInventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryHttpClient> _logger;

    public InventoryHttpClient(HttpClient httpClient, ILogger<InventoryHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ReserveStockResponse> ReserveStockAsync(ReserveStockRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/inventory/reserve", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict ||
            response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Inventory reserve failed: {Status} {Body}", response.StatusCode, problem);
            throw new ValidationException($"Inventory reservation failed: {problem}");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReserveStockResponse>(cancellationToken)
            ?? new ReserveStockResponse { Success = false, Message = "Empty inventory response." };
    }

    public async Task ReleaseStockAsync(ReleaseStockRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/inventory/release", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public class NotificationHttpPublisher : IIntegrationEventPublisher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationHttpPublisher> _logger;

    public NotificationHttpPublisher(HttpClient httpClient, ILogger<NotificationHttpPublisher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task PublishOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken cancellationToken = default)
        => PublishAsync("/api/events/order-created", evt, cancellationToken);

    public Task PublishOrderStatusChangedAsync(OrderStatusChangedEvent evt, CancellationToken cancellationToken = default)
        => PublishAsync("/api/events/order-status-changed", evt, cancellationToken);

    public Task PublishOrderCancelledAsync(OrderCancelledEvent evt, CancellationToken cancellationToken = default)
        => PublishAsync("/api/events/order-cancelled", evt, cancellationToken);

    private async Task PublishAsync<T>(string path, T payload, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(path, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to publish event to {Path}: {Status}", path, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Do not fail the order transaction if notification is unavailable
            _logger.LogError(ex, "Error publishing integration event to {Path}", path);
        }
    }
}
