using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Posts.Queries.CountPostsByAuthor;

namespace Venture.Api.Endpoints.Internal;

public sealed class InternalPostsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("internal/authors/{authorId:guid}/posts/count", async (
                Guid authorId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new CountPostsByAuthorQuery(authorId), cancellationToken);
                return result.Match(value => Results.Ok(new { count = value }));
            })
            .WithTags("Internal");
    }
}
