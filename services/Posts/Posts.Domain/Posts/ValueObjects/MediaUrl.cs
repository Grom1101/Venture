using Venture.Domain.Abstractions.ValueObjects;
using Venture.Domain.Common.Results;

namespace Venture.Domain.Posts.ValueObjects;

public sealed class MediaUrl : ValueObject
{
    public const int MaxLength = 2048;

    public string Value { get; }

    private MediaUrl(string value)
    {
        Value = value;
    }

    public static Result<MediaUrl> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<MediaUrl>(Error.Conflict("Posts.MediaUrl.Empty", "ссылка на медиа не может быть пустой"));

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<MediaUrl>(Error.Conflict(
                "Posts.MediaUrl.TooLong",
                $"Ссылка на медиа не должна превышать {MaxLength} символов"));

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return Result.Failure<MediaUrl>(Error.Conflict(
                "Posts.MediaUrl.Invalid",
                "ссылка на медиа должна быть абсолютным http(s) адресом"));

        return Result.Success(new MediaUrl(trimmed));
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
