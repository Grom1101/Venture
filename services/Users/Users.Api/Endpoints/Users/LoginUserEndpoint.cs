using MediatR;
using Venture.Api.Extensions;
using Venture.Application.Users.Commands.LoginUser;

namespace Venture.Api.Endpoints.Users;

public sealed class LoginUserEndpoint : IEndpoint
{
    public sealed record Request(string Email, string Password);

    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("users/login", async (Request request, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new LoginUserCommand(request.Email, request.Password), cancellationToken);

                return result.Match(token => Results.Ok(new { accessToken = token }));
            })
            .WithTags("Users")
            .AllowAnonymous();
}
