using Microsoft.Extensions.Logging;
using MiniCommerce.Contracts.Events;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Services;

public class NotificationAppService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationAppService> _logger;

    public NotificationAppService(INotificationRepository repository, ILogger<NotificationAppService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<NotificationDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Notification '{id}' was not found.");
        return Map(item);
    }

    public async Task HandleOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken cancellationToken = default)
    {
        await CreateAndSendAsync(
            evt.OrderId,
            evt.Email,
            $"Order {evt.OrderNumber} confirmed",
            $"Hi {evt.CustomerName}, your order {evt.OrderNumber} for {evt.Quantity} x {evt.ProductSku} (total ${evt.TotalAmount:F2}) has been created.",
            NotificationType.OrderCreated,
            cancellationToken);
    }

    public async Task HandleOrderStatusChangedAsync(OrderStatusChangedEvent evt, CancellationToken cancellationToken = default)
    {
        await CreateAndSendAsync(
            evt.OrderId,
            evt.Email,
            $"Order {evt.OrderNumber} status updated",
            $"Hi {evt.CustomerName}, your order {evt.OrderNumber} status changed from {evt.PreviousStatus} to {evt.NewStatus}.",
            NotificationType.OrderStatusChanged,
            cancellationToken);
    }

    public async Task HandleOrderCancelledAsync(OrderCancelledEvent evt, CancellationToken cancellationToken = default)
    {
        await CreateAndSendAsync(
            evt.OrderId,
            evt.Email,
            $"Order {evt.OrderNumber} cancelled",
            $"Your order {evt.OrderNumber} has been cancelled. Reserved stock for {evt.ProductSku} will be released.",
            NotificationType.OrderCancelled,
            cancellationToken);
    }

    private async Task CreateAndSendAsync(
        Guid orderId,
        string email,
        string subject,
        string body,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            RecipientEmail = email,
            Subject = subject,
            Body = body,
            Type = type,
            Status = NotificationStatus.Pending,
            CreatedDate = utcNow
        };

        try
        {
            // Placeholder for SMTP / SendGrid / Azure Communication Services
            _logger.LogInformation("Sending notification to {Email}: {Subject}", email, subject);
            notification.Status = NotificationStatus.Sent;
            notification.SentDate = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to {Email}", email);
            notification.Status = NotificationStatus.Failed;
        }

        await _repository.CreateAsync(notification, cancellationToken);
    }

    private static NotificationDto Map(Notification n) => new()
    {
        Id = n.Id,
        OrderId = n.OrderId,
        RecipientEmail = n.RecipientEmail,
        Subject = n.Subject,
        Body = n.Body,
        Type = n.Type.ToString(),
        Status = n.Status.ToString(),
        CreatedDate = n.CreatedDate,
        SentDate = n.SentDate
    };
}
