using Venture.Application.Abstractions.Persistence.Models;

namespace Venture.Application.Abstractions.Persistence;

/// <summary>
/// Read-side для CQRS: запросы проецируют данные в DTO и никогда не загружают агрегаты целиком.
/// </summary>
public interface IReadDbContext
{
    IQueryable<Notification> Notifications { get; }
}
