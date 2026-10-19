using Venture.Domain.Social.Follows;

namespace Venture.Application.Abstractions.Persistence;

/// <summary>
/// Read-side для CQRS: запросы проецируют данные в DTO и никогда не загружают агрегаты целиком.
/// </summary>
public interface IReadDbContext
{
    IQueryable<Follow> Follows { get; }
}
