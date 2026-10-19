using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Users.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(string Username) : IQuery<UserProfileDto>;
