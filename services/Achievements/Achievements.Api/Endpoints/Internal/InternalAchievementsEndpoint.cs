using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Achievements.Queries.GetAchievementGroups;

namespace Venture.Api.Endpoints.Internal;

public sealed class InternalAchievementsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("internal/users/{userId:guid}/achievement-groups", async (
                Guid userId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAchievementGroupsQuery(userId), cancellationToken);
                return result.Match(Results.Ok);
            })
            .WithTags("Internal");
    }
}
