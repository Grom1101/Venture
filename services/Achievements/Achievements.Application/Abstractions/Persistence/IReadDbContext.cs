using Venture.Domain.Achievements;

namespace Venture.Application.Abstractions.Persistence;

/// <summary>
/// Read-side для CQRS: запросы проецируют данные в DTO и никогда не загружают агрегаты целиком.
/// </summary>
public interface IReadDbContext
{
    IQueryable<Achievement> Achievements { get; }

    IQueryable<UserAchievementProgress> UserAchievementStates { get; }
}
