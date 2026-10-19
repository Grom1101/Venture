using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Users.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    string DisplayName,
    string About,
    string FocusedOn,
    string? Comment,
    IReadOnlyCollection<string> Interests) : ICommand;
