using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Achievements.Facts;

public sealed record ObserveUserFollowedCommand(
    string EventId,
    Guid FollowerId,
    Guid FollowingId,
    int SchemaVersion) : ICommand;
