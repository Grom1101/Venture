using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Achievements;

namespace Venture.Infrastructure.Persistence.Repositories;

internal sealed class AchievementRepository(VentureDbContext dbContext) : IAchievementRepository
{
    public Task<Achievement?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        dbContext.Achievements.FirstOrDefaultAsync(
            achievement => achievement.Name == name,
            cancellationToken);

    public void Add(Achievement achievement) => dbContext.Achievements.Add(achievement);
}
