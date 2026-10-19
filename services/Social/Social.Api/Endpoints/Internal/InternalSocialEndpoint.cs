using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Social.Queries.CountFollowers;

namespace Venture.Api.Endpoints.Internal;

public sealed class InternalSocialEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("internal/users/{userId:guid}/followers/count", async (
                Guid userId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new CountFollowersQuery(userId), cancellationToken);
                return result.Match(value => Results.Ok(new { count = value }));
            })
            .WithTags("Internal");
    }
}
