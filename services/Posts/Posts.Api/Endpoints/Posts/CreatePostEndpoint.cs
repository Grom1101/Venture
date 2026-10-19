using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Posts.Commands.CreatePost;

namespace Venture.Api.Endpoints.Posts;

public sealed class CreatePostEndpoint : IEndpoint
{
    public sealed record Request(string Content);

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("posts", async (Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new CreatePostCommand(request.Content), cancellationToken);

                return result.Match(postId => Results.Created($"/posts/{postId}", postId));
            })
            .WithTags("Posts")
            .RequireAuthorization();
}
