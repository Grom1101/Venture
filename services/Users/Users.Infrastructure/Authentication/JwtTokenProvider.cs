using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Venture.Application.Abstractions.Authentication;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Authentication;

internal sealed class JwtTokenProvider(IOptions<JwtOptions> options) : ITokenProvider
{
    public string GenerateAccessToken(UserId userId, Username username, Email email)
    {
        var jwtOptions = options.Value;

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username.Value),
                new Claim(JwtRegisteredClaimNames.Email, email.Value)
            ]),
            Expires = DateTime.UtcNow.AddMinutes(jwtOptions.ExpirationInMinutes),
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            SigningCredentials = signingCredentials
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
