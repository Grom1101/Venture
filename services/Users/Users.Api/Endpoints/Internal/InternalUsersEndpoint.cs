using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Users.Queries.GetUserCard;

namespace Venture.Api.Endpoints.Internal;

public sealed class InternalUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("internal/users/{userId:guid}", async (
                Guid userId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetUserCardQuery(userId), cancellationToken);
                return result.Match(Results.Ok);
            })
            .WithTags("Internal");
    }
}
