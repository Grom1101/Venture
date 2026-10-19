using Microsoft.Extensions.Logging;
using Venture.Application.Abstractions.Time;
using Venture.Application.Integration;
using Venture.Infrastructure.Messaging;

namespace Venture.Infrastructure.Integration;

internal sealed class KafkaPostFacts(
    KafkaProducer producer,
    IDateTimeProvider dateTimeProvider,
    ILogger<KafkaPostFacts> logger) : IPostFacts
{
    public async Task PublishPostPublishedAsync(
        Guid postId,
        Guid authorId,
        CancellationToken cancellationToken)
    {
        var occurredAtUtc = DateTime.SpecifyKind(dateTimeProvider.UtcNow, DateTimeKind.Utc);
        var publication = PostPublishedFact.Plan(postId, authorId, occurredAtUtc);

        await producer.PublishAsync(
            publication.Topic,
            publication.Key,
            publication.Message,
            cancellationToken);

        logger.LogInformation(
            "Факт {EventId} пользователя {UserId} записан в топик {Topic}",
            publication.Message.EventId,
            authorId,
            publication.Topic);
    }
}
