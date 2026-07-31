namespace GestionEspaces.Application.Common;

/// <summary>
/// Represents the result of an operation that does not return a value.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, IReadOnlyCollection<ErrorDetail> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyCollection<ErrorDetail> Errors { get; }

    public static Result Success() => new(true, Array.Empty<ErrorDetail>());

    public static Result Failure(params ErrorDetail[] errors) => new(false, errors);

    public static Result Failure(IEnumerable<ErrorDetail> errors) => new(false, errors.ToArray());
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
public sealed class Result<T> : Result
{
    private Result(T? value, bool isSuccess, IReadOnlyCollection<ErrorDetail> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value, true, Array.Empty<ErrorDetail>());

    public new static Result<T> Failure(params ErrorDetail[] errors) => new(default, false, errors);

    public new static Result<T> Failure(IEnumerable<ErrorDetail> errors) => new(default, false, errors.ToArray());
}