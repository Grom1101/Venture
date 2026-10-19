using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Users.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password) : ICommand<string>;
