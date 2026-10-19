using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Notifications;
using Venture.Application.Abstractions.Persistence.Models;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Repositories;

internal sealed class NotificationWriter(VentureDbContext dbContext) : INotificationWriter
{
    public async Task AddAsync(
        UserId recipientId,
        NotificationType type,
        UserId? actorId,
        Guid? referenceId,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        // Повторная доставка из Kafka приходит с теми же полями.
        var alreadyStored = await dbContext.Notifications.AnyAsync(
            notification =>
                notification.RecipientId == recipientId &&
                notification.Type == type &&
                notification.ActorId == actorId &&
                notification.ReferenceId == referenceId &&
                notification.CreatedAtUtc == createdAtUtc,
            cancellationToken);

        if (alreadyStored)
            return;

        dbContext.Notifications.Add(new Notification(recipientId, type, actorId, referenceId, createdAtUtc));
    }
}
