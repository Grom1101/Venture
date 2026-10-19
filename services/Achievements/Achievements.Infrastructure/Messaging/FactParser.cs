using System.Text.Json;
using Venture.Application.Achievements.Facts;

namespace Venture.Infrastructure.Messaging;

internal static class FactParser
{
    private static readonly Dictionary<string, Func<string, ParsedFact>> _topicActions = new ()
    {
        [KafkaTopics.PostPublished] = ParsePost,
        [KafkaTopics.UserFollowed] = ParseFollow
    };

    public static ParsedFact Parse(string topic, string payload)
    {
        if (_topicActions.TryGetValue(topic, out var parse))
        {
            return parse(payload);
        }

        throw new InvalidOperationException($"Unexpected Kafka topic '{topic}'.");
    }

    private static ParsedFact ParsePost(string payload)
    {
        var message = JsonSerializer.Deserialize<PostPublishedMessage>(payload, KafkaJson.Options)
            ?? throw new InvalidFactException("Пустое тело сообщения.");

        if (message.SchemaVersion != 1)
            throw new UnsupportedFactSchemaException(message.SchemaVersion, message.EventId.ToString("D"));

        if (message.EventId == Guid.Empty || message.PostId == Guid.Empty || message.AuthorId == Guid.Empty)
            throw new InvalidFactException("В факте поста нет eventId, postId или authorId.");

        if (message.EventId != message.PostId)
            throw new InvalidFactException("eventId факта поста должен совпадать с postId.");

        return new ParsedFact(
            new ObservePostPublishedCommand(
                message.EventId.ToString("D"),
                message.PostId,
                message.AuthorId,
                message.SchemaVersion),
            message.EventId.ToString("D"),
            message.AuthorId);
    }

    private static ParsedFact ParseFollow(string payload)
    {
        var message = JsonSerializer.Deserialize<UserFollowedMessage>(payload, KafkaJson.Options)
            ?? throw new InvalidFactException("Пустое тело сообщения.");

        if (message.SchemaVersion != 1)
            throw new UnsupportedFactSchemaException(message.SchemaVersion, message.EventId);

        if (message.FollowerId == Guid.Empty || message.FollowingId == Guid.Empty || string.IsNullOrWhiteSpace(message.EventId))
            throw new InvalidFactException("В факте подписки нет eventId, followerId или followingId.");

        var expectedEventId = $"{message.FollowingId:D}:{message.FollowerId:D}";

        if (!string.Equals(message.EventId, expectedEventId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidFactException("eventId факта подписки должен быть парой followingId:followerId.");

        return new ParsedFact(
            new ObserveUserFollowedCommand(
                message.EventId,
                message.FollowerId,
                message.FollowingId,
                message.SchemaVersion),
            message.EventId,
            message.FollowingId);
    }
}

internal sealed record ParsedFact(
    Venture.Application.Abstractions.Messaging.ICommand Command,
    string EventId,
    Guid UserId);
