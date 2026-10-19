using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Posts.Commands.DeletePost;

public sealed record DeletePostCommand(Guid PostId) : ICommand;
