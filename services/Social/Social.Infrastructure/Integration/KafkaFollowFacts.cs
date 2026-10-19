using Microsoft.Extensions.Logging;
using Venture.Application.Abstractions.Time;
using Venture.Application.Integration;
using Venture.Infrastructure.Messaging;

namespace Venture.Infrastructure.Integration;

internal sealed class KafkaFollowFacts(
    KafkaProducer producer,
    IDateTimeProvider dateTimeProvider,
    ILogger<KafkaFollowFacts> logger) : IFollowFacts
{
    public async Task PublishUserFollowedAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken)
    {
        var occurredAtUtc = DateTime.SpecifyKind(dateTimeProvider.UtcNow, DateTimeKind.Utc);
        var publication = UserFollowedFact.Plan(followerId, followingId, occurredAtUtc);

        await producer.PublishAsync(
            publication.Topic,
            publication.Key,
            publication.Message,
            cancellationToken);

        logger.LogInformation(
            "Факт {EventId} пользователя {UserId} записан в топик {Topic}",
            publication.Message.EventId,
            followingId,
            publication.Topic);
    }
}
