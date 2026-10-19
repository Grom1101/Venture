using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.Enums;

namespace Venture.Domain.Users.ValueObjects;

public sealed class UserSkill : ValueObject
{
    public SkillName Name { get; }

    public SkillLevel Level { get; }

    private UserSkill(SkillName name, SkillLevel level)
    {
        Name = name;
        Level = level;
    }

    internal static Result<UserSkill> Create(SkillName name, SkillLevel level)
    {
        if (name is null)
            return Result.Failure<UserSkill>(Error.Conflict("Users.SkillName.Empty", "скилл не должен быть пустым"));

        return Result.Success(new UserSkill(name, level));
    }

    public UserSkill ChangeLevel(SkillLevel level) => new(Name, level);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
    }
}
