using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Domain.Common.Results;
using Venture.Domain.Users;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Users.Commands.RegisterUser;

internal sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.IsFailure)
            return Result.Failure<Guid>(email.Error);

        var username = Username.Create(command.Username);
        if (username.IsFailure)
            return Result.Failure<Guid>(username.Error);

        var displayName = DisplayName.Create(command.DisplayName);
        if (displayName.IsFailure)
            return Result.Failure<Guid>(displayName.Error);

        if (await userRepository.IsUsernameTakenAsync(username.Value, cancellationToken))
            return Result.Failure<Guid>(UserErrors.UsernameAlreadyTaken);

        if (await userRepository.IsEmailTakenAsync(email.Value, cancellationToken))
            return Result.Failure<Guid>(UserErrors.EmailAlreadyTaken);

        var passwordHash = PasswordHash.FromHash(passwordHasher.Hash(command.Password));
        if (passwordHash.IsFailure)
            return Result.Failure<Guid>(passwordHash.Error);

        var user = User.Register(
            username.Value,
            email.Value,
            passwordHash.Value,
            displayName.Value,
            dateTimeProvider.UtcNow);

        if (user.IsFailure)
            return Result.Failure<Guid>(user.Error);

        userRepository.Add(user.Value);

        return Result.Success(user.Value.Id.Value);
    }
}
