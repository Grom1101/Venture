using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Social.Commands.FollowUser;

public sealed record FollowUserCommand(Guid UserId) : ICommand;
