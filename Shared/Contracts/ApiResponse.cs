using System.Text.Json.Serialization;

namespace Shared.Contracts;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? ErrorCode,
    IReadOnlyList<string>? Errors,
    DateTime Timestamp
)
{
    public static ApiResponse<T> Ok(T data) => new(true, data, null, null, DateTime.UtcNow);
    public static ApiResponse<T> Fail(string? errorCode, IEnumerable<string>? errors) =>
        new(false, default, errorCode, errors?.ToArray(), DateTime.UtcNow);
}

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data) => ApiResponse<T>.Ok(data);
    public static ApiResponse<T> Fail<T>(string? errorCode, IEnumerable<string>? errors) => ApiResponse<T>.Fail(errorCode, errors);
}
