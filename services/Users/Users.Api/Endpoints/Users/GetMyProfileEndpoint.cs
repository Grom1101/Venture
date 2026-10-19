using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Users.Queries.GetMyProfile;

namespace Venture.Api.Endpoints.Users;

public sealed class GetMyProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("users/me", async (ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetMyProfileQuery(), cancellationToken);

                return result.Match(Results.Ok);
            })
            .WithTags("Users")
            .RequireAuthorization();
}
