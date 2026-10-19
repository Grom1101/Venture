namespace Venture.Application.Users.Queries.GetUserProfile;

public sealed record UserProfileDto(
    Guid Id,
    string Username,
    string DisplayName,
    UserBioDto Bio,
    int FollowersCount,
    int FollowingCount,
    IReadOnlyCollection<UserSkillDto> Skills,
    IReadOnlyCollection<UserAchievementDto> Achievements,
    DateTime CreatedAtUtc);

public sealed record UserBioDto(
    string About,
    string FocusedOn,
    string Comment,
    IReadOnlyCollection<string> Interests);

public sealed record UserSkillDto(string Name, string Level);

public sealed record UserAchievementDto(
    Guid Id,
    string Type,
    string Name,
    string Description,
    DateTime EarnedOnUtc);
