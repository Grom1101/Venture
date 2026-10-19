using Venture.Application.Abstractions.Messaging;
using Venture.Domain.Users.Enums;

namespace Venture.Application.Users.Commands.AddSkill;

public sealed record AddSkillCommand(string Name, SkillLevel Level) : ICommand;
