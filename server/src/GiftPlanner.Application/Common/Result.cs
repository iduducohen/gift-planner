namespace GiftPlanner.Application.Common;

public enum ErrorType
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4,
    Unavailable = 5
}

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error NotFound(string what) => new("not_found", $"{what} not found.", ErrorType.NotFound);
    public static Error Validation(string message) => new("validation", message, ErrorType.Validation);
    public static Error Conflict(string message) => new("conflict", message, ErrorType.Conflict);
}

/// <summary>Use cases return results instead of throwing for expected failures. The API maps them to ProblemDetails.</summary>
public class Result
{
    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    protected Result(Error? error) => Error = error;

    public static Result Ok() => new(null);
    public static Result Fail(Error error) => new(error);
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    public static implicit operator Result(Error error) => Fail(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("No value on a failed result.");

    private Result(T? value, Error? error) : base(error) => _value = value;

    public static Result<T> Ok(T value) => new(value, null);
    public new static Result<T> Fail(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail(error);
}
