using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Posts.Commands.DeletePost;

namespace Venture.Api.Endpoints.Posts;

public sealed class DeletePostEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("posts/{postId:guid}",
                async (Guid postId, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new DeletePostCommand(postId), cancellationToken);

                    return result.Match(Results.NoContent);
                })
            .WithTags("Posts")
            .RequireAuthorization();
}
