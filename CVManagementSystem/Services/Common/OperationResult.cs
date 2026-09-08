namespace CVManagementSystem.Services.Common;

public class OperationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public bool IsConflict { get; init; }

    public static OperationResult Ok() => new() { Success = true };
    public static OperationResult Fail(string error) => new() { Success = false, Error = error };
    public static OperationResult Conflict(string? error = null) => new()
    {
        Success = false,
        IsConflict = true,
        Error = error ?? "The record was modified by someone else. Please reload and try again."
    };
}

public class OperationResult<T> : OperationResult
{
    public T? Value { get; init; }

    public static OperationResult<T> Ok(T value) => new() { Success = true, Value = value };
    public new static OperationResult<T> Fail(string error) => new() { Success = false, Error = error };
    public new static OperationResult<T> Conflict(string? error = null) => new()
    {
        Success = false,
        IsConflict = true,
        Error = error ?? "The record was modified by someone else. Please reload and try again."
    };
}