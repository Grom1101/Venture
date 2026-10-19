using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Users;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(VentureDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken) =>
        dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken) =>
        dbContext.Users.FirstOrDefaultAsync(user => user.Username == username, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.Id == id, cancellationToken);

    public Task<bool> IsUsernameTakenAsync(Username username, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.Username == username, cancellationToken);

    public Task<bool> IsEmailTakenAsync(Email email, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);
}
