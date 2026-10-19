using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Users.Queries.GetUserProfile;

namespace Venture.Api.Endpoints.Users;

public sealed class GetUserProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("users/{username}", async (string username, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetUserProfileQuery(username), cancellationToken);

                return result.Match(Results.Ok);
            })
            .WithTags("Users")
            .AllowAnonymous();
}
