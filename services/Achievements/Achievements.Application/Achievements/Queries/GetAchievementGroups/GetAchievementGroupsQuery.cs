using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Achievements.Queries.GetAchievementGroups;

public sealed record AchievementGroupDto(
    string Type,
    IReadOnlyCollection<AchievementCatalogItemDto> Achievements);

public sealed record AchievementCatalogItemDto(
    Guid Id,
    string Name,
    string Description,
    DateTime? EarnedOnUtc);

public sealed record GetAchievementGroupsQuery(Guid UserId)
    : IQuery<IReadOnlyCollection<AchievementGroupDto>>;

internal sealed class GetAchievementGroupsQueryHandler(IReadDbContext readDbContext)
    : IQueryHandler<GetAchievementGroupsQuery, IReadOnlyCollection<AchievementGroupDto>>
{
    public async Task<Result<IReadOnlyCollection<AchievementGroupDto>>> Handle(
        GetAchievementGroupsQuery query,
        CancellationToken cancellationToken)
    {
        var catalog = await readDbContext.Achievements
            .Select(achievement => new
            {
                achievement.Id,
                achievement.AchievementType,
                achievement.Name,
                achievement.Description
            })
            .ToListAsync(cancellationToken);

        var earnedOn = await EarnedOnByAchievementAsync(query.UserId, cancellationToken);

        var groups = catalog
            .GroupBy(item => item.AchievementType)
            .OrderBy(group => group.Key)
            .Select(group => new AchievementGroupDto(
                group.Key.ToString(),
                group
                    .OrderBy(item => item.Name)
                    .Select(item => new AchievementCatalogItemDto(
                        item.Id.Value,
                        item.Name,
                        item.Description,
                        earnedOn.TryGetValue(item.Id.Value, out var earned) ? earned : null))
                    .ToArray()))
            .ToArray();

        return Result.Success<IReadOnlyCollection<AchievementGroupDto>>(groups);
    }

    private async Task<Dictionary<Guid, DateTime>> EarnedOnByAchievementAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            return [];

        var parsed = UserId.From(userId);
        if (parsed.IsFailure)
            return [];

        var earned = await readDbContext.UserAchievementStates
            .Where(state => state.Id == parsed.Value)
            .SelectMany(state => state.Earned)
            .Select(item => new { item.AchievementId, item.EarnedOnUtc })
            .ToListAsync(cancellationToken);

        return earned.ToDictionary(item => item.AchievementId.Value, item => item.EarnedOnUtc);
    }
}
