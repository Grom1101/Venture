using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Users.ValueObjects;

public class Bio : ValueObject
{
    public const int MaxAboutLength = 100;
    private const int MinAboutLength = 3;

    public const int MaxInterestsCount = 10;

    public const int MaxFocusedOnLength = 200;
    private const int MinFocusedOnLength = 3;

    public const int MaxCommentLength = 500;

    private const string DefaultText = "Информация не задана";


    public string About { get; }

    private readonly Interest[] _interests;
    public IReadOnlyList<Interest> Interests => _interests;

    public string FocusedOn { get; }

    public string Comment { get; }

    private Bio(string about, string focusedOn, string comment, IEnumerable<Interest> interests)
    {
        About = about;
        FocusedOn = focusedOn;
        Comment = comment;
        _interests = interests?.ToArray() ?? [];
    }

    public static Result<Bio> Create(string about, string focusedOn, string comment, IEnumerable<Interest> interests)
    {
        if (interests is null)
            return Result.Failure<Bio>(Error.Conflict("Users.Bio.Interests", "интересы не заданы"));

        var validAbout = ValidateAbout(about);
        if (validAbout.IsFailure)
            return Result.Failure<Bio>(validAbout.Error);

        var validFocusedOn = ValidateFocusedOn(focusedOn);
        if (validFocusedOn.IsFailure)
            return Result.Failure<Bio>(validFocusedOn.Error);

        var validComment = ValidateComment(comment);
        if (validComment.IsFailure)
            return Result.Failure<Bio>(validComment.Error);

        return Result.Success(new Bio(validAbout.Value, validFocusedOn.Value, validComment.Value, interests));
    }

    public static Bio Default() =>
        Create(DefaultText, DefaultText, string.Empty, []).Value;

    public Result<Bio> AddInterest(Interest interest)
    {
        if (_interests.Length >= MaxInterestsCount)
            return Result.Failure<Bio>(Error.Conflict(
                "Users.Bio.TooManyInterests",
                $"Нельзя добавить более {MaxInterestsCount} интересов"));

        if (interest is null)
            return Result.Failure<Bio>(Error.Conflict("Users.Interest.Empty", "интерес не должен быть пустым"));

        if (_interests.Contains(interest))
            return Result.Success(this);

        var interests = _interests.ToList();
        interests.Add(interest);

        return Result.Success(new Bio(About, FocusedOn, Comment, interests));
    }

    public Bio RemoveInterest(Interest interest)
    {
        var interests = _interests.ToList();

        if (!interests.Remove(interest))
            return this;

        return new Bio(About, FocusedOn, Comment, interests);
    }


    public Result<Bio> ChangeAbout(string about)
    {
        var validAbout = ValidateAbout(about);
        if (validAbout.IsFailure)
            return Result.Failure<Bio>(validAbout.Error);

        return Result.Success(new Bio(validAbout.Value, FocusedOn, Comment, Interests));
    }

    public Result<Bio> ChangeFocusedOn(string focusedOn)
    {
        var validFocusedOn = ValidateFocusedOn(focusedOn);
        if (validFocusedOn.IsFailure)
            return Result.Failure<Bio>(validFocusedOn.Error);

        return Result.Success(new Bio(About, validFocusedOn.Value, Comment, Interests));
    }

    public Result<Bio> ChangeComment(string comment)
    {
        var validComment = ValidateComment(comment);
        if (validComment.IsFailure)
            return Result.Failure<Bio>(validComment.Error);

        return Result.Success(new Bio(About, FocusedOn, validComment.Value, Interests));
    }

    private static Result<string> ValidateAbout(string about)
    {
        if (string.IsNullOrWhiteSpace(about))
            return Result.Failure<string>(Error.Conflict(
                "Users.Bio.AboutLength",
                $"About должно содержать от {MinAboutLength} до {MaxAboutLength} символов"));

        var normalized = about.Trim();

        if (normalized.Length > MaxAboutLength || normalized.Length < MinAboutLength)
            return Result.Failure<string>(Error.Conflict(
                "Users.Bio.AboutLength",
                $"About должно содержать от {MinAboutLength} до {MaxAboutLength} символов"));

        return Result.Success(normalized);
    }

    private static Result<string> ValidateFocusedOn(string focusedOn)
    {
        if (string.IsNullOrWhiteSpace(focusedOn))
            return Result.Failure<string>(Error.Conflict(
                "Users.Bio.FocusedOnLength",
                $"FocusedOn должно содержать от {MinFocusedOnLength} до {MaxFocusedOnLength} символов"));

        var normalized = focusedOn.Trim();

        if (normalized.Length > MaxFocusedOnLength || normalized.Length < MinFocusedOnLength)
            return Result.Failure<string>(Error.Conflict(
                "Users.Bio.FocusedOnLength",
                $"FocusedOn должно содержать от {MinFocusedOnLength} до {MaxFocusedOnLength} символов"));

        return Result.Success(normalized);
    }

    private static Result<string> ValidateComment(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return Result.Success(DefaultText);

        var normalized = comment.Trim();

        if (normalized.Length > MaxCommentLength)
            return Result.Failure<string>(Error.Conflict(
                "Users.Bio.CommentTooLong",
                $"Comment не должен превышать {MaxCommentLength} символов"));

        return Result.Success(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return About;
        yield return FocusedOn;
        yield return Comment;

        foreach (var interest in _interests)
            yield return interest;
    }
}
