using Venture.Domain.Achievements;

namespace Venture.Application.Abstractions.Persistence;

public interface IAchievementRepository
{
    Task<Achievement?> GetByNameAsync(string name, CancellationToken cancellationToken);

    void Add(Achievement achievement);
}
