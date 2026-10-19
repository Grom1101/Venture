using Venture.Domain.Abstractions.Aggregates;
using Venture.Domain.Achievements.ValueObjects;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Achievements;

/// <summary>
/// Сколько фактов Achievements уже принял по пользователю и какие значки выдал.
/// Повтор факта сюда не доходит: его отсекает учёт EventId до вызова этих методов.
/// </summary>
public sealed class UserAchievementProgress : AggregateRoot<UserId>
{
    private readonly List<EarnedAchievement> _earned = [];

    public int PostsObserved { get; private set; }

    public int FollowersObserved { get; private set; }

    public IReadOnlyCollection<EarnedAchievement> Earned => _earned.AsReadOnly();

    private UserAchievementProgress()
    {
    }

    public static Result<UserAchievementProgress> Start(UserId userId)
    {
        if (userId is null)
            return Result.Failure<UserAchievementProgress>(Error.Conflict(
                "Users.UserId.Empty",
                "StronglyTypedId cannot be an empty guid."));

        return Result.Success(new UserAchievementProgress { Id = userId });
    }

    public Result ObservePost(AchievementId firstPostAchievementId, DateTime earnedOnUtc)
    {
        if (firstPostAchievementId is null)
            return Result.Failure(Error.Conflict(
                "Achievements.AchievementId.Empty",
                "StronglyTypedId cannot be an empty guid."));

        PostsObserved++;
        return GrantIfReached(firstPostAchievementId, PostsObserved, earnedOnUtc);
    }

    public Result ObserveFollower(AchievementId firstFollowerAchievementId, DateTime earnedOnUtc)
    {
        if (firstFollowerAchievementId is null)
            return Result.Failure(Error.Conflict(
                "Achievements.AchievementId.Empty",
                "StronglyTypedId cannot be an empty guid."));

        FollowersObserved++;
        return GrantIfReached(firstFollowerAchievementId, FollowersObserved, earnedOnUtc);
    }

    public bool Has(AchievementId achievementId) =>
        _earned.Any(earned => earned.AchievementId == achievementId);

    private Result GrantIfReached(AchievementId achievementId, int observed, DateTime earnedOnUtc)
    {
        if (observed < 1 || Has(achievementId))
            return Result.Success();

        var earned = EarnedAchievement.Grant(achievementId, earnedOnUtc);
        if (earned.IsFailure)
            return Result.Failure(earned.Error);

        _earned.Add(earned.Value);
        return Result.Success();
    }
}
