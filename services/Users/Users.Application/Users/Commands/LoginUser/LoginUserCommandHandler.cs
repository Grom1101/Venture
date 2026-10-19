using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Users.Commands.LoginUser;

internal sealed class LoginUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider)
    : ICommandHandler<LoginUserCommand, string>
{
    public async Task<Result<string>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.IsFailure)
            return Result.Failure<string>(email.Error);

        var user = await userRepository.GetByEmailAsync(email.Value, cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash.Value))
            return Result.Failure<string>(UserErrors.InvalidCredentials);

        var accessToken = tokenProvider.GenerateAccessToken(user.Id, user.Username, user.Email);

        return Result.Success(accessToken);
    }
}
