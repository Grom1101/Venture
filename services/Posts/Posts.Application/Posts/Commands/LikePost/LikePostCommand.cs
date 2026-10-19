using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Posts.Commands.LikePost;

public sealed record LikePostCommand(Guid PostId) : ICommand;
