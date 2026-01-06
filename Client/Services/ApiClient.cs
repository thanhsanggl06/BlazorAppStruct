using System.Net.Http.Json;
using Shared.Contracts;

namespace Client.Services;

public interface IApiClient
{
    Task<ApiResponse<T>> GetAsync<T>(string url, CancellationToken ct = default);
    Task<ApiResponse<T>> PostAsync<T>(string url, object body, CancellationToken ct = default);
    Task<ApiResponse<T>> PutAsync<T>(string url, object body, CancellationToken ct = default);
    Task<ApiResponse<T>> DeleteAsync<T>(string url, CancellationToken ct = default);
    
    // Download methods
    Task<FileDownloadResult> DownloadFileAsync(string url, object? body = null, CancellationToken ct = default);
    Task<MultipleFilesDownloadResult> DownloadMultipleFilesAsync(string url, object body, CancellationToken ct = default);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string url, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Get, url, null, ct);

    public async Task<ApiResponse<T>> PostAsync<T>(string url, object body, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Post, url, body, ct);

    public async Task<ApiResponse<T>> PutAsync<T>(string url, object body, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Put, url, body, ct);

    public async Task<ApiResponse<T>> DeleteAsync<T>(string url, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Delete, url, null, ct);

    /// <summary>
    /// Download m?t file t? server (s? d?ng POST method)
    /// </summary>
    public async Task<FileDownloadResult> DownloadFileAsync(string url, object? body = null, CancellationToken ct = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            if (body is not null)
                req.Content = JsonContent.Create(body);

            var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!res.IsSuccessStatusCode)
            {
                var errorMsg = await res.Content.ReadAsStringAsync(ct);
                return new FileDownloadResult
                {
                    Success = false,
                    ErrorMessage = $"Download failed: {(int)res.StatusCode} - {errorMsg}"
                };
            }

            // Ð?c file content
            var fileBytes = await res.Content.ReadAsByteArrayAsync(ct);

            // L?y thông tin file t? headers
            var fileName = GetFileNameFromHeaders(res) ?? "download.bin";
            var contentType = res.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            return new FileDownloadResult
            {
                Success = true,
                FileBytes = fileBytes,
                FileName = fileName,
                ContentType = contentType
            };
        }
        catch (Exception ex)
        {
            return new FileDownloadResult
            {
                Success = false,
                ErrorMessage = $"Download exception: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Download nhi?u files t? server (thý?ng tr? v? ZIP file)
    /// </summary>
    public async Task<MultipleFilesDownloadResult> DownloadMultipleFilesAsync(string url, object body, CancellationToken ct = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Content = JsonContent.Create(body);

            var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!res.IsSuccessStatusCode)
            {
                var errorMsg = await res.Content.ReadAsStringAsync(ct);
                return new MultipleFilesDownloadResult
                {
                    Success = false,
                    ErrorMessage = $"Download failed: {(int)res.StatusCode} - {errorMsg}"
                };
            }

            // Ð?c ZIP content
            var zipBytes = await res.Content.ReadAsByteArrayAsync(ct);

            // L?y thông tin ZIP file
            var fileName = GetFileNameFromHeaders(res) ?? "downloads.zip";
            var contentType = res.Content.Headers.ContentType?.MediaType ?? "application/zip";

            // L?y metadata v? s? files (n?u có trong custom headers)
            var fileCount = GetFileCountFromHeaders(res);

            return new MultipleFilesDownloadResult
            {
                Success = true,
                ZipBytes = zipBytes,
                ZipFileName = fileName,
                ContentType = contentType,
                FileCount = fileCount
            };
        }
        catch (Exception ex)
        {
            return new MultipleFilesDownloadResult
            {
                Success = false,
                ErrorMessage = $"Download exception: {ex.Message}"
            };
        }
    }

    private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, url);
        if (body is not null)
            req.Content = JsonContent.Create(body);

        var res = await _http.SendAsync(req, ct);

        try
        {
            var payload = await res.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: ct);
            if (payload is not null)
                return payload;
        }
        catch { }

        var fallbackMessage = res.IsSuccessStatusCode ? "Empty response" : $"Request failed: {(int)res.StatusCode}";
        return ApiResponse.Error<T>(fallbackMessage, res.StatusCode.ToString());
    }

    private static string? GetFileNameFromHeaders(HttpResponseMessage response)
    {
        // Th? l?y t? Content-Disposition header
        if (response.Content.Headers.ContentDisposition?.FileName is { } fileName)
        {
            // Remove quotes n?u có
            return fileName.Trim('"');
        }

        // Th? l?y t? custom header
        if (response.Headers.TryGetValues("X-File-Name", out var values))
        {
            return values.FirstOrDefault();
        }

        return null;
    }

    private static int? GetFileCountFromHeaders(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-File-Count", out var values))
        {
            if (int.TryParse(values.FirstOrDefault(), out var count))
                return count;
        }
        return null;
    }
}

/// <summary>
/// K?t qu? download 1 file
/// </summary>
public record FileDownloadResult
{
    public bool Success { get; init; }
    public byte[]? FileBytes { get; init; }
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// K?t qu? download nhi?u files
/// </summary>
public record MultipleFilesDownloadResult
{
    public bool Success { get; init; }
    public byte[]? ZipBytes { get; init; }
    public string? ZipFileName { get; init; }
    public string? ContentType { get; init; }
    public int? FileCount { get; init; }
    public string? ErrorMessage { get; init; }
}
