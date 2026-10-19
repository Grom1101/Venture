using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Posts.Commands.UnlikePost;

public sealed record UnlikePostCommand(Guid PostId) : ICommand;
