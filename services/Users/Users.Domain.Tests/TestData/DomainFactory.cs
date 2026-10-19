using Venture.Domain.Users;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Tests.TestData;

internal static class DomainFactory
{
    public static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static User User(
        string username = "johndoe",
        string email = "john@doe.com",
        string displayName = "John Doe") =>
        Domain.Users.User.Register(
            Username.Create(username).Value,
            Email.Create(email).Value,
            PasswordHash.FromHash("salt.hash").Value,
            DisplayName.Create(displayName).Value,
            Now).Value;
}
