using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Posts.Queries.CountPostsByAuthor;

public sealed record CountPostsByAuthorQuery(Guid AuthorId) : IQuery<int>;

internal sealed class CountPostsByAuthorQueryHandler(IReadDbContext readDbContext)
    : IQueryHandler<CountPostsByAuthorQuery, int>
{
    public async Task<Result<int>> Handle(CountPostsByAuthorQuery query, CancellationToken cancellationToken)
    {
        var authorId = UserId.From(query.AuthorId);
        if (authorId.IsFailure)
            return Result.Failure<int>(authorId.Error);

        var count = await readDbContext.Posts.CountAsync(
            post => post.AuthorId == authorId.Value && !post.IsDeleted,
            cancellationToken);

        return Result.Success(count);
    }
}
