using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Events;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Abstractions.Events;
using Venture.Domain.Achievements;

namespace Venture.Infrastructure.Persistence;

public sealed class VentureDbContext(
    DbContextOptions<VentureDbContext> options,
    IDomainEventDispatcher domainEventDispatcher)
    : DbContext(options), IUnitOfWork, IReadDbContext
{
    public DbSet<Achievement> Achievements => Set<Achievement>();

    public DbSet<UserAchievementProgress> UserAchievementStates => Set<UserAchievementProgress>();

    public DbSet<ProcessedFact> ProcessedFacts => Set<ProcessedFact>();

    IQueryable<Achievement> IReadDbContext.Achievements => Achievements.AsNoTracking();

    IQueryable<UserAchievementProgress> IReadDbContext.UserAchievementStates =>
        UserAchievementStates.AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VentureDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// События собираются до сохранения, а публикуются после него: обработчики уже видят
    /// зафиксированное состояние и работают внутри той же транзакции.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .SelectMany(aggregate => aggregate.ClearDomainEvents())
            .ToArray();

        var affected = await base.SaveChangesAsync(cancellationToken);

        if (domainEvents.Length == 0)
            return affected;

        await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        if (ChangeTracker.HasChanges())
            affected += await base.SaveChangesAsync(cancellationToken);

        return affected;
    }
}
