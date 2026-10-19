using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Achievements.Queries.GetUserAchievements;

public sealed record UserAchievementDisplayDto(
    Guid Id,
    string Type,
    string Name,
    string Description,
    DateTime EarnedOnUtc);

public sealed record GetUserAchievementsQuery(Guid UserId)
    : IQuery<IReadOnlyCollection<UserAchievementDisplayDto>>;

internal sealed class GetUserAchievementsQueryHandler(IReadDbContext readDbContext)
    : IQueryHandler<GetUserAchievementsQuery, IReadOnlyCollection<UserAchievementDisplayDto>>
{
    public async Task<Result<IReadOnlyCollection<UserAchievementDisplayDto>>> Handle(
        GetUserAchievementsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
            return Result.Success<IReadOnlyCollection<UserAchievementDisplayDto>>([]);

        var userId = UserId.From(query.UserId).Value;

        var achievements = await readDbContext.UserAchievementStates
            .Where(state => state.Id == userId)
            .SelectMany(state => state.Earned)
            .Join(
                readDbContext.Achievements,
                earned => earned.AchievementId,
                achievement => achievement.Id,
                (earned, achievement) => new { earned, achievement })
            .OrderByDescending(row => row.earned.EarnedOnUtc)
            .Select(row => new UserAchievementDisplayDto(
                row.achievement.Id.Value,
                row.achievement.AchievementType.ToString(),
                row.achievement.Name,
                row.achievement.Description,
                row.earned.EarnedOnUtc))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<UserAchievementDisplayDto>>(achievements);
    }
}
