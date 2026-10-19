using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Authentication;

public interface ITokenProvider
{
    string GenerateAccessToken(UserId userId, Username username, Email email);
}
