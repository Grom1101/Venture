using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Users.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Username,
    string DisplayName,
    string Password) : ICommand<Guid>;
