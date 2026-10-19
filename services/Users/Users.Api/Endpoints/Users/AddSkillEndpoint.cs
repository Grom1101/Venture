using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Users.Commands.AddSkill;
using Venture.Domain.Users.Enums;

namespace Venture.Api.Endpoints.Users;

public sealed class AddSkillEndpoint : IEndpoint
{
    public sealed record Request(string Name, SkillLevel Level);

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("users/me/skills", async (Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new AddSkillCommand(request.Name, request.Level), cancellationToken);

                return result.Match(Results.NoContent);
            })
            .WithTags("Users")
            .RequireAuthorization();
}
