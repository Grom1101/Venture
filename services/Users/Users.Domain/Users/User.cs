using Venture.Domain.Abstractions.Aggregates;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.Enums;
using Venture.Domain.Users.Events;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Domain.Users;

public class User : AggregateRoot<UserId>
{
    private const int MaxSkillsCount = 30;

    private readonly List<UserSkill> _skills = [];

    public Username Username { get; private set; }

    public Email Email { get; private set; }

    public PasswordHash PasswordHash { get; private set; }

    public DisplayName DisplayName { get; private set; }

    public Bio Bio { get; private set; }

    public IReadOnlyCollection<UserSkill> Skills => _skills.AsReadOnly();

    public DateTime CreatedAtUtc { get; private set; }

    private User()
    {
        Username = null!;
        Email = null!;
        PasswordHash = null!;
        DisplayName = null!;
        Bio = null!;
    }

    private User(
        UserId id,
        Username username,
        Email email,
        PasswordHash passwordHash,
        DisplayName displayName,
        Bio bio,
        IEnumerable<UserSkill>? skills,
        DateTime createdAtUtc)
    {
        Id = id;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Bio = bio;
        CreatedAtUtc = createdAtUtc;

        if (skills is not null)
            _skills.AddRange(skills);
    }

    public static Result<User> Register(
        Username username,
        Email email,
        PasswordHash passwordHash,
        DisplayName displayName,
        DateTime createdAtUtc,
        Bio? bio = null,
        IEnumerable<UserSkill>? skills = null)
    {
        if (username is null)
            return Result.Failure<User>(Error.Conflict("Users.Username.Empty", "имя не может быть пустым"));

        if (email is null)
            return Result.Failure<User>(Error.Conflict("Users.Email.Empty", "почта не должна быть пустой"));

        if (passwordHash is null)
            return Result.Failure<User>(Error.Conflict("Users.PasswordHash.Empty", "хеш пароля не может быть пустым"));

        if (displayName is null)
            return Result.Failure<User>(Error.Conflict("Users.DisplayName.Empty", "отображаемое имя не может быть пустым"));

        var userId = UserId.New();

        var user = new User(
            userId,
            username,
            email,
            passwordHash,
            displayName,
            bio ?? Bio.Default(),
            skills,
            createdAtUtc);

        user.AddDomainEvent(new UserRegisteredDomainEvent(userId, username));

        return Result.Success(user);
    }

    public Result ChangeUsername(Username username)
    {
        if (username is null)
            return Result.Failure(Error.Conflict("Users.Username.Empty", "имя не может быть пустым"));

        if (Username == username)
            return Result.Success();

        Username = username;
        AddDomainEvent(new UserProfileChangedDomainEvent(Id, username));
        return Result.Success();
    }

    public Result ChangeProfile(DisplayName displayName, Bio bio)
    {
        if (displayName is null)
            return Result.Failure(Error.Conflict("Users.DisplayName.Empty", "отображаемое имя не может быть пустым"));

        if (bio is null)
            return Result.Failure(Error.Conflict("Users.Bio.Missing", "профиль не задан"));

        if (DisplayName == displayName && Bio == bio)
            return Result.Success();

        DisplayName = displayName;
        Bio = bio;

        AddDomainEvent(new UserProfileChangedDomainEvent(Id, Username));
        return Result.Success();
    }

    public Result ChangeDisplayName(DisplayName displayName)
    {
        if (displayName is null)
            return Result.Failure(Error.Conflict("Users.DisplayName.Empty", "отображаемое имя не может быть пустым"));

        DisplayName = displayName;
        return Result.Success();
    }

    public Result ChangeBio(Bio bio)
    {
        if (bio is null)
            return Result.Failure(Error.Conflict("Users.Bio.Missing", "профиль не задан"));

        Bio = bio;
        return Result.Success();
    }

    public Result ChangePassword(PasswordHash passwordHash)
    {
        if (passwordHash is null)
            return Result.Failure(Error.Conflict("Users.PasswordHash.Empty", "хеш пароля не может быть пустым"));

        PasswordHash = passwordHash;
        return Result.Success();
    }

    public Result AddSkill(SkillName skillName, SkillLevel level)
    {
        if (skillName is null)
            return Result.Failure(Error.Conflict("Users.SkillName.Empty", "скилл не должен быть пустым"));

        if (_skills.Count >= MaxSkillsCount)
            return Result.Failure(Error.Conflict(
                "Users.Skills.TooMany",
                $"нельзя добавить более {MaxSkillsCount} навыков"));

        var skill = UserSkill.Create(skillName, level);
        if (skill.IsFailure)
            return Result.Failure(skill.Error);

        if (_skills.Contains(skill.Value))
            return Result.Failure(Error.Conflict("Users.Skills.Duplicate", "пользователь уже имеет этот навык"));

        _skills.Add(skill.Value);
        AddDomainEvent(new UserProfileChangedDomainEvent(Id, Username));
        return Result.Success();
    }

    public Result RemoveSkill(SkillName skillName)
    {
        if (skillName is null)
            return Result.Failure(Error.Conflict("Users.SkillName.Empty", "скилл не должен быть пустым"));

        var existing = _skills.FirstOrDefault(skill => skill.Name == skillName);

        if (existing is null)
            return Result.Failure(Error.Conflict("Users.Skills.NotFound", "навык не найден"));

        _skills.Remove(existing);
        AddDomainEvent(new UserProfileChangedDomainEvent(Id, Username));
        return Result.Success();
    }
}
