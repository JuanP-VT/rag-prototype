namespace RagPrototype.Domain.Common;

/// <summary>
/// Result pattern — business errors returned as values, not exceptions.
/// Exceptions are for infrastructure failures (DB down, network timeout).
/// Expected business failures (wrong password, not found) use Result.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
        => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read value of a failed result.");
}

public record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"{entity} with id '{id}' was not found.");

    public static Error Conflict(string description) =>
        new("Conflict", description);

    public static Error Validation(string field, string description) =>
        new($"Validation.{field}", description);

    public static Error Unauthorized(string description = "Invalid credentials.") =>
        new("Unauthorized", description);

    public static Error LlmUnavailable(string description) =>
    new("Llm.Unavailable", description);
}
