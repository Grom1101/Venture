using Venture.Domain.Common.Results;

namespace Venture.Application.Common.Results;

public sealed record ValidationError : Error
{
    public ValidationError(IReadOnlyCollection<Error> errors)
        : base("Validation.General", "Одна или несколько проверок не пройдены", ErrorType.Validation)
    {
        Errors = errors;
    }

    public IReadOnlyCollection<Error> Errors { get; }
}
