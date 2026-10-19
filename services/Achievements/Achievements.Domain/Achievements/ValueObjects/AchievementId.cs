using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Achievements.ValueObjects;

public sealed class AchievementId : StronglyTypedId
{
    private AchievementId(Guid value) : base(value) { }

    public static AchievementId New() => new(Guid.NewGuid());

    public static Result<AchievementId> From(Guid value) =>
        Create(value, id => new AchievementId(id), "Achievements.AchievementId.Empty");
}
