using System.Text.Json.Serialization;

namespace Shared.Contracts;

public class ApiResponse<T>
{
    public string Status { get; init; } = "success"; // success | fail | error
    public T? Data { get; init; }
    public string? Message { get; init; }
    public string? Code { get; init; }

    [JsonIgnore]
    public bool IsSuccess => string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase);
    [JsonIgnore]
    public bool IsFail => string.Equals(Status, "fail", StringComparison.OrdinalIgnoreCase);
    [JsonIgnore]
    public bool IsError => string.Equals(Status, "error", StringComparison.OrdinalIgnoreCase);
}

public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data) => new() { Status = "success", Data = data };

    public static ApiResponse<T> Fail<T>(T data) => new() { Status = "fail", Data = data };
    public static ApiResponse<T> Fail<T>(string message, string? code = null)
        => new() { Status = "fail", Message = message, Code = code };
    public static ApiResponse<object> Fail(string message, string? code = null)
        => new() { Status = "fail", Message = message, Code = code };

    public static ApiResponse<T> Error<T>(string message, string? code = null)
        => new() { Status = "error", Message = message, Code = code };
}
