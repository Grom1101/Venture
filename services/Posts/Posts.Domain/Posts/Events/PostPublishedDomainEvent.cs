using Venture.Domain.Abstractions.Events;
using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Posts.Events;

public sealed record PostPublishedDomainEvent(
    PostId PostId,
    UserId AuthorId
    ) : DomainEvent;
