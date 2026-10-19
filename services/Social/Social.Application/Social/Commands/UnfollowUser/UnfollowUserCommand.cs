using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Social.Commands.UnfollowUser;

public sealed record UnfollowUserCommand(Guid UserId) : ICommand;
