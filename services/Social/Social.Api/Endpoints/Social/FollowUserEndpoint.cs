using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Social.Commands.FollowUser;
using Venture.Application.Social.Commands.UnfollowUser;

namespace Venture.Api.Endpoints.Social;

public sealed class FollowUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{userId:guid}/follow",
                async (Guid userId, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new FollowUserCommand(userId), cancellationToken);

                    return result.Match(Results.NoContent);
                })
            .WithTags("Social")
            .RequireAuthorization();

        app.MapDelete("users/{userId:guid}/follow",
                async (Guid userId, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new UnfollowUserCommand(userId), cancellationToken);

                    return result.Match(Results.NoContent);
                })
            .WithTags("Social")
            .RequireAuthorization();
    }
}
