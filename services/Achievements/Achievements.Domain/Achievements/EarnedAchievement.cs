using Venture.Domain.Achievements.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Achievements;

/// <summary>
/// Выданное достижение. Пара пользователь + достижение уникальна, время — момент решения Achievements.
/// </summary>
public sealed class EarnedAchievement
{
    public AchievementId AchievementId { get; private set; } = null!;

    public DateTime EarnedOnUtc { get; private set; }

    private EarnedAchievement()
    {
    }

    private EarnedAchievement(AchievementId achievementId, DateTime earnedOnUtc)
    {
        AchievementId = achievementId;
        EarnedOnUtc = earnedOnUtc;
    }

    internal static Result<EarnedAchievement> Grant(AchievementId achievementId, DateTime earnedOnUtc)
    {
        if (achievementId is null)
            return Result.Failure<EarnedAchievement>(Error.Conflict(
                "Achievements.AchievementId.Empty",
                "StronglyTypedId cannot be an empty guid."));

        return Result.Success(new EarnedAchievement(achievementId, earnedOnUtc));
    }
}
