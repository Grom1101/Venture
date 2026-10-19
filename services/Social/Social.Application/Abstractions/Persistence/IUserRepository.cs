using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken);
}
