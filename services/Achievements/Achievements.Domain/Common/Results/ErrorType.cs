namespace Venture.Domain.Common.Results;

public enum ErrorType
{
    Failure = 0,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}
