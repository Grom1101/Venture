using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Posts.Commands.LikePost;
using Venture.Application.Posts.Commands.UnlikePost;

namespace Venture.Api.Endpoints.Posts;

public sealed class LikePostEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("posts/{postId:guid}/likes",
                async (Guid postId, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new LikePostCommand(postId), cancellationToken);

                    return result.Match(Results.NoContent);
                })
            .WithTags("Posts")
            .RequireAuthorization();

        app.MapDelete("posts/{postId:guid}/likes",
                async (Guid postId, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new UnlikePostCommand(postId), cancellationToken);

                    return result.Match(Results.NoContent);
                })
            .WithTags("Posts")
            .RequireAuthorization();
    }
}
