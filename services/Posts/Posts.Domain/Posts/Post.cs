using Venture.Domain.Abstractions.Aggregates;
using Venture.Domain.Common.Results;
using Venture.Domain.Posts.Entities;
using Venture.Domain.Posts.Events;
using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Posts;

public class Post : AggregateRoot<PostId>
{
    private const int MaxMediaCount = 10;

    private readonly List<PostMedia> _media = [];

    public UserId AuthorId { get; private set; }

    public PostContent Content { get; private set; }

    public IReadOnlyCollection<PostMedia> Media => _media.AsReadOnly();

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? EditedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    private Post()
    {
        AuthorId = null!;
        Content = null!;
    }

    private Post(PostId id, UserId authorId, PostContent content, DateTime createdAtUtc)
    {
        Id = id;
        AuthorId = authorId;
        Content = content;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// В первой версии пост публикуется сразу — черновиков нет.
    /// </summary>
    public static Result<Post> Publish(UserId authorId, PostContent content, DateTime createdAtUtc)
    {
        if (authorId is null)
            return Result.Failure<Post>(Error.Conflict("Posts.Author.Missing", "автор поста не задан"));

        if (content is null)
            return Result.Failure<Post>(Error.Conflict("Posts.Content.Empty", "пост не может быть пустым"));

        var post = new Post(PostId.New(), authorId, content, createdAtUtc);

        post.AddDomainEvent(new PostPublishedDomainEvent(post.Id, authorId));

        return Result.Success(post);
    }

    public Result Edit(UserId editorId, PostContent content, DateTime editedAtUtc)
    {
        if (editorId is null)
            return Result.Failure(Error.Conflict("Posts.Author.Missing", "автор поста не задан"));

        if (content is null)
            return Result.Failure(Error.Conflict("Posts.Content.Empty", "пост не может быть пустым"));

        var author = EnsureAuthor(editorId);
        if (author.IsFailure)
            return author;

        var deleted = EnsureNotDeleted();
        if (deleted.IsFailure)
            return deleted;

        Content = content;
        EditedAtUtc = editedAtUtc;
        return Result.Success();
    }

    public Result Delete(UserId requesterId)
    {
        if (requesterId is null)
            return Result.Failure(Error.Conflict("Posts.Author.Missing", "автор поста не задан"));

        var author = EnsureAuthor(requesterId);
        if (author.IsFailure)
            return author;

        var deleted = EnsureNotDeleted();
        if (deleted.IsFailure)
            return deleted;

        IsDeleted = true;
        return Result.Success();
    }

    public Result AddMedia(MediaUrl url)
    {
        if (url is null)
            return Result.Failure(Error.Conflict("Posts.MediaUrl.Empty", "ссылка на медиа не может быть пустой"));

        var deleted = EnsureNotDeleted();
        if (deleted.IsFailure)
            return deleted;

        if (_media.Count >= MaxMediaCount)
            return Result.Failure(Error.Conflict(
                "Posts.TooManyMedia",
                $"к посту нельзя прикрепить более {MaxMediaCount} медиа"));

        if (_media.Any(media => media.Url == url))
            return Result.Failure(Error.Conflict("Posts.DuplicateMedia", "это медиа уже прикреплено к посту"));

        var media = PostMedia.Create(url, _media.Count);
        if (media.IsFailure)
            return Result.Failure(media.Error);

        _media.Add(media.Value);
        return Result.Success();
    }

    private Result EnsureAuthor(UserId userId)
    {
        if (AuthorId != userId)
            return Result.Failure(Error.Conflict("Posts.NotAuthor", "изменять пост может только его автор"));

        return Result.Success();
    }

    private Result EnsureNotDeleted()
    {
        if (IsDeleted)
            return Result.Failure(Error.Conflict("Posts.Deleted", "пост удалён"));

        return Result.Success();
    }
}
