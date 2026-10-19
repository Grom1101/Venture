using Venture.Domain.Abstractions.Aggregates;
using Venture.Domain.Achievements.Enums;
using Venture.Domain.Achievements.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Achievements;

/// <summary>
/// Каталог достижений. Прогресс и выданные значки живут в Achievements, не в Users.
/// </summary>
public class Achievement : AggregateRoot<AchievementId>
{
    private const int MinNameLength = 2;
    private const int MaxNameLength = 50;
    private const int MaxDescriptionLength = 200;

    public AchievementType AchievementType { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public DateTime? LastModifiedOnUtc { get; private set; }

    private Achievement()
    {
        Name = null!;
        Description = null!;
    }

    private Achievement(
        AchievementId id,
        AchievementType achievementType,
        string name,
        string description,
        DateTime createdOnUtc)
    {
        Id = id;
        AchievementType = achievementType;
        Name = name;
        Description = description;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<Achievement> Create(
        AchievementType achievementType,
        string name,
        string description,
        DateTime createdOnUtc)
    {
        if (achievementType == AchievementType.Unknown)
            return Result.Failure<Achievement>(Error.Conflict(
                "Achievements.TypeRequired",
                "тип достижения должен быть задан"));

        var validName = NormalizeName(name);
        if (validName.IsFailure)
            return Result.Failure<Achievement>(validName.Error);

        var validDescription = NormalizeDescription(description);
        if (validDescription.IsFailure)
            return Result.Failure<Achievement>(validDescription.Error);

        return Result.Success(new Achievement(
            AchievementId.New(),
            achievementType,
            validName.Value,
            validDescription.Value,
            createdOnUtc));
    }

    public Result ChangeName(string name, DateTime modifiedOnUtc)
    {
        var validName = NormalizeName(name);
        if (validName.IsFailure)
            return Result.Failure(validName.Error);

        Name = validName.Value;
        LastModifiedOnUtc = modifiedOnUtc;
        return Result.Success();
    }

    public Result ChangeDescription(string description, DateTime modifiedOnUtc)
    {
        var validDescription = NormalizeDescription(description);
        if (validDescription.IsFailure)
            return Result.Failure(validDescription.Error);

        Description = validDescription.Value;
        LastModifiedOnUtc = modifiedOnUtc;
        return Result.Success();
    }

    private static Result<string> NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<string>(Error.Conflict(
                "Achievements.NameLength",
                $"Название достижения не должно быть более {MaxNameLength} символов и не менее {MinNameLength}"));

        var normalized = name.Trim();

        if (normalized.Length > MaxNameLength || normalized.Length < MinNameLength)
            return Result.Failure<string>(Error.Conflict(
                "Achievements.NameLength",
                $"Название достижения не должно быть более {MaxNameLength} символов и не менее {MinNameLength}"));

        return Result.Success(normalized);
    }

    private static Result<string> NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<string>(Error.Conflict(
                "Achievements.Description.Empty",
                "Описание не должно быть пустым"));

        var normalized = description.Trim();

        if (normalized.Length > MaxDescriptionLength)
            return Result.Failure<string>(Error.Conflict(
                "Achievements.DescriptionTooLong",
                $"Описание должно быть не более {MaxDescriptionLength} символов"));

        return Result.Success(normalized);
    }
}
