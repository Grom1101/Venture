using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Posts.Commands.CreatePost;

public sealed record CreatePostCommand(string Content) : ICommand<Guid>;
