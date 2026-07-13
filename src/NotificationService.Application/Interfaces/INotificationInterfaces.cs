using MiniCommerce.Contracts.Events;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<NotificationDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task HandleOrderCreatedAsync(OrderCreatedEvent evt, CancellationToken cancellationToken = default);
    Task HandleOrderStatusChangedAsync(OrderStatusChangedEvent evt, CancellationToken cancellationToken = default);
    Task HandleOrderCancelledAsync(OrderCancelledEvent evt, CancellationToken cancellationToken = default);
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? SentDate { get; set; }
}
