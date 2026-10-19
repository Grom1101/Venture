using Venture.Domain.Users;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Tests.TestData;

internal static class ApplicationFactory
{
    public static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static User User(
        string username = "johndoe",
        string email = "john@doe.com",
        string passwordHash = "salt.hash") =>
        Domain.Users.User.Register(
            Username.Create(username).Value,
            Email.Create(email).Value,
            PasswordHash.FromHash(passwordHash).Value,
            DisplayName.Create("John Doe").Value,
            Now).Value;
}
