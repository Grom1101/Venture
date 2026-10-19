using Venture.Application.Abstractions.Notifications;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Persistence.Models;

/// <summary>
/// Уведомление в первой версии — тоже простая persistence-модель без доменного поведения.
/// </summary>
public sealed class Notification
{
    public Guid Id { get; private set; }

    public UserId RecipientId { get; private set; }

    public NotificationType Type { get; private set; }

    public UserId? ActorId { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public bool IsRead { get; private set; }

    private Notification()
    {
        RecipientId = null!;
    }

    public Notification(
        UserId recipientId,
        NotificationType type,
        UserId? actorId,
        Guid? referenceId,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        RecipientId = recipientId;
        Type = type;
        ActorId = actorId;
        ReferenceId = referenceId;
        CreatedAtUtc = createdAtUtc;
    }

    public void MarkAsRead() => IsRead = true;
}
