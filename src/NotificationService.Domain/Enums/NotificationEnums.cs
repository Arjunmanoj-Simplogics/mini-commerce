namespace NotificationService.Domain.Enums;

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

public enum NotificationType
{
    OrderCreated = 0,
    OrderStatusChanged = 1,
    OrderCancelled = 2
}
