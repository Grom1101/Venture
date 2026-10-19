using Venture.Application.Abstractions.Integration;
using Venture.Application.Abstractions.Notifications;
using Venture.Domain.Users.ValueObjects;
using Venture.Infrastructure.Messaging;

namespace Venture.Infrastructure.Integration;

internal sealed class KafkaNotificationWriter(KafkaProducer producer, IPostCommitActions postCommitActions)
    : INotificationWriter
{
    public Task AddAsync(
        UserId recipientId,
        NotificationType type,
        UserId? actorId,
        Guid? referenceId,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        var message = new NotificationMessage(
            recipientId.Value,
            type.ToString(),
            actorId?.Value,
            referenceId,
            createdAtUtc);

        postCommitActions.Enqueue(token =>
            producer.PublishAsync(
                KafkaTopics.Notifications,
                recipientId.Value.ToString("D"),
                message,
                token));

        return Task.CompletedTask;
    }
}
