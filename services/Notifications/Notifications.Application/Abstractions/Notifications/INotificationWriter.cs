using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Notifications;

/// <summary>
/// Уведомление — простая persistence-модель, а не агрегат, поэтому Application
/// пишет его через узкую абстракцию.
/// </summary>
public interface INotificationWriter
{
    Task AddAsync(
        UserId recipientId,
        NotificationType type,
        UserId? actorId,
        Guid? referenceId,
        DateTime createdAtUtc,
        CancellationToken cancellationToken);
}
