using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Achievements;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Repositories;

internal sealed class AchievementProgressStore(VentureDbContext dbContext) : IAchievementProgressStore
{
    public async Task<bool> TryAcceptAsync(
        string eventId,
        DateTime acceptedOnUtc,
        CancellationToken cancellationToken)
    {
        var inserted = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "ProcessedFacts" ("EventId", "AcceptedOnUtc")
             VALUES ({eventId}, {acceptedOnUtc})
             ON CONFLICT ("EventId") DO NOTHING
             """,
            cancellationToken);

        return inserted == 1;
    }

    public async Task<UserAchievementProgress> LockAsync(UserId userId, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "UserAchievementStates" ("UserId", "PostsObserved", "FollowersObserved")
             VALUES ({userId.Value}, 0, 0)
             ON CONFLICT ("UserId") DO NOTHING
             """,
            cancellationToken);

        if (LocksRows(dbContext.Database.ProviderName))
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT "UserId" FROM "UserAchievementStates" WHERE "UserId" = {userId.Value} FOR UPDATE""",
                cancellationToken);
        }

        return await dbContext.UserAchievementStates
            .Include(progress => progress.Earned)
            .SingleAsync(progress => progress.Id == userId, cancellationToken);
    }

    internal static bool LocksRows(string? providerName) =>
        providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
}
