using Venture.Domain.Abstractions.Events;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Social.Follows.Events;

public sealed record UserFollowedDomainEvent(
    UserId FollowerId,
    UserId FollowingId
    ) : DomainEvent;
