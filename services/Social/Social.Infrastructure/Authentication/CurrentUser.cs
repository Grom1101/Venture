using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Venture.Application.Abstractions.Authentication;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Authentication;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public UserId? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;

            var rawId = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(rawId, out var id))
                return null;

            var userId = Domain.Users.ValueObjects.UserId.From(id);
            return userId.IsSuccess ? userId.Value : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;
}
