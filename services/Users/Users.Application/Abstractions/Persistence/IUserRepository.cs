using Venture.Domain.Users;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken);

    Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken);

    Task<bool> IsUsernameTakenAsync(Username username, CancellationToken cancellationToken);

    Task<bool> IsEmailTakenAsync(Email email, CancellationToken cancellationToken);

    void Add(User user);
}
