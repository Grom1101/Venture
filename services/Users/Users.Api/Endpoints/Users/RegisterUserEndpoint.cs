using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Users.Commands.RegisterUser;

namespace Venture.Api.Endpoints.Users;

public sealed class RegisterUserEndpoint : IEndpoint
{
    public sealed record Request(string Email, string Username, string DisplayName, string Password);

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("users", async (Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new RegisterUserCommand(
                    request.Email,
                    request.Username,
                    request.DisplayName,
                    request.Password);

                var result = await sender.Send(command, cancellationToken);

                return result.Match(userId => Results.Created($"/users/{request.Username}", userId));
            })
            .WithTags("Users")
            .AllowAnonymous();
}
