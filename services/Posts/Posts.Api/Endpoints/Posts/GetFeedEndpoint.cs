using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Posts.Queries.GetFeed;

namespace Venture.Api.Endpoints.Posts;

public sealed class GetFeedEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("feed", async (
                ISender sender,
                CancellationToken cancellationToken,
                int page = 1,
                int pageSize = 20) =>
            {
                var result = await sender.Send(new GetFeedQuery(page, pageSize), cancellationToken);

                return result.Match(Results.Ok);
            })
            .WithTags("Posts")
            .RequireAuthorization();
}
