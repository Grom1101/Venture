using Venture.Application.Abstractions.Messaging;
using Venture.Application.Users.Queries.GetUserProfile;

namespace Venture.Application.Users.Queries.GetMyProfile;

public sealed record GetMyProfileQuery : IQuery<UserProfileDto>;
