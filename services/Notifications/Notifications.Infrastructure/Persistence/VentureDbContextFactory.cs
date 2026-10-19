using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Venture.Application.Abstractions.Events;
using Venture.Domain.Abstractions.Events;
using Venture.Infrastructure.Persistence;

namespace Venture.Infrastructure.Persistence;

public sealed class VentureDbContextFactory : IDesignTimeDbContextFactory<VentureDbContext>
{
    public VentureDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VentureDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=venture_notifications;Username=postgres;Password=postgres")
            .Options;

        return new VentureDbContext(options, new DesignTimeDomainEventDispatcher());
    }

    private sealed class DesignTimeDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
