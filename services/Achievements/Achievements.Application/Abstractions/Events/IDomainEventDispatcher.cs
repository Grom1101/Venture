using Venture.Domain.Abstractions.Events;

namespace Venture.Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
