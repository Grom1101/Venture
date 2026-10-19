namespace Venture.Infrastructure.Messaging;

internal sealed record PostPublishedMessage(
    int SchemaVersion,
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid PostId,
    Guid AuthorId);

internal readonly record struct KafkaPublication<T>(string Topic, string Key, T Message);

/// <summary>
/// Факт «пост опубликован». EventId совпадает с postId, ключ Kafka — authorId.
/// </summary>
internal static class PostPublishedFact
{
    public const int SchemaVersion = 1;

    public static KafkaPublication<PostPublishedMessage> Plan(
        Guid postId,
        Guid authorId,
        DateTime occurredAtUtc)
    {
        var message = new PostPublishedMessage(
            SchemaVersion,
            EventId: postId,
            OccurredAtUtc: occurredAtUtc,
            PostId: postId,
            AuthorId: authorId);

        return new KafkaPublication<PostPublishedMessage>(
            KafkaTopics.PostPublished,
            authorId.ToString("D"),
            message);
    }
}
