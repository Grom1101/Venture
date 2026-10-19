using Venture.Application.Abstractions.Persistence.Models;
using Venture.Domain.Posts;

namespace Venture.Application.Abstractions.Persistence;

/// <summary>
/// Read-side для CQRS: запросы проецируют данные в DTO и никогда не загружают агрегаты целиком.
/// </summary>
public interface IReadDbContext
{
    IQueryable<Post> Posts { get; }

    IQueryable<PostLike> PostLikes { get; }
}
