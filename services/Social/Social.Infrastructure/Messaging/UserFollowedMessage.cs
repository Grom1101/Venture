namespace Venture.Infrastructure.Messaging;

internal sealed record UserFollowedMessage(
    int SchemaVersion,
    string EventId,
    DateTime OccurredAtUtc,
    Guid FollowerId,
    Guid FollowingId);

internal readonly record struct KafkaPublication<T>(string Topic, string Key, T Message);

/// <summary>
/// Факт «пользователь подписался». EventId — пара followingId:followerId из согласованного JSON.
/// Ключ Kafka — followingId, то есть тот, на кого подписались.
/// </summary>
internal static class UserFollowedFact
{
    public const int SchemaVersion = 1;

    public static string EventId(Guid followerId, Guid followingId) =>
        $"{followingId:D}:{followerId:D}";

    public static KafkaPublication<UserFollowedMessage> Plan(
        Guid followerId,
        Guid followingId,
        DateTime occurredAtUtc)
    {
        var message = new UserFollowedMessage(
            SchemaVersion,
            EventId(followerId, followingId),
            occurredAtUtc,
            followerId,
            followingId);

        return new KafkaPublication<UserFollowedMessage>(
            KafkaTopics.UserFollowed,
            followingId.ToString("D"),
            message);
    }
}
