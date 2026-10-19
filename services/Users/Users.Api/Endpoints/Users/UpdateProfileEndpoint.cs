using MediatR;
using Microsoft.AspNetCore.Authorization;
using Venture.Api.Extensions;
using Venture.Application.Users.Commands.UpdateProfile;

namespace Venture.Api.Endpoints.Users;

public sealed class UpdateProfileEndpoint : IEndpoint
{
    public sealed record Request(
        string DisplayName,
        string About,
        string FocusedOn,
        string? Comment,
        IReadOnlyCollection<string>? Interests);

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPatch("users/me", async (Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new UpdateProfileCommand(
                    request.DisplayName,
                    request.About,
                    request.FocusedOn,
                    request.Comment,
                    request.Interests ?? []);

                var result = await sender.Send(command, cancellationToken);

                return result.Match(Results.NoContent);
            })
            .WithTags("Users")
            .RequireAuthorization();
}
