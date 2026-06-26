namespace QtBank.Api.Application.Common;

/// <summary>
/// Represents the result of an operation, containing either a value or an error message.
/// </summary>
/// <typeparam name="T">The type of the returned value.</typeparam>
public readonly record struct Result<T>(T? Value, string? Error, bool IsSuccess)
{
    public static Result<T> Ok(T value) => new(value, null, true);
    public static Result<T> Fail(string error) => new(default, error, false);
}
