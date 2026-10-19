using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Posts.ValueObjects;

public sealed class PostContent : ValueObject
{
    public const int MaxLength = 5000;

    public string Value { get; }

    private PostContent(string value)
    {
        Value = value;
    }

    public static Result<PostContent> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<PostContent>(Error.Conflict("Posts.Content.Empty", "пост не может быть пустым"));

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<PostContent>(Error.Conflict(
                "Posts.Content.TooLong",
                $"Пост не должен превышать {MaxLength} символов"));

        return Result.Success(new PostContent(trimmed));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
