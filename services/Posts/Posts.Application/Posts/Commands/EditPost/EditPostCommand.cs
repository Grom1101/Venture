using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Posts.Commands.EditPost;

public sealed record EditPostCommand(Guid PostId, string Content) : ICommand;
