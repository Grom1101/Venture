using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Posts.Commands.EditPost;

namespace Venture.Api.Endpoints.Posts;

public sealed class EditPostEndpoint : IEndpoint
{
    public sealed record Request(string Content);

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("posts/{postId:guid}",
                async (Guid postId, Request request, ISender sender, CancellationToken cancellationToken) =>
                {
                    var result = await sender.Send(new EditPostCommand(postId, request.Content), cancellationToken);

                    return result.Match(Results.NoContent);
                })
            .WithTags("Posts")
            .RequireAuthorization();
}
