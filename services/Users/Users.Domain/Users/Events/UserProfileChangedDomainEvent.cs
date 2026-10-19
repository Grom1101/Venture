using Venture.Domain.Abstractions.Events;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Users.Events;

public sealed record UserProfileChangedDomainEvent(
    UserId UserId,
    Username Username
    ) : DomainEvent;
