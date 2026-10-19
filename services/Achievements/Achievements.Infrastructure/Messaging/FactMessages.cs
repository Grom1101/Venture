namespace Venture.Infrastructure.Messaging;

internal sealed record PostPublishedMessage(
    int SchemaVersion,
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid PostId,
    Guid AuthorId);

internal sealed record UserFollowedMessage(
    int SchemaVersion,
    string EventId,
    DateTime OccurredAtUtc,
    Guid FollowerId,
    Guid FollowingId);
