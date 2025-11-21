using System.Net.Http.Json;
using Shared.Contracts;

namespace Client.Services;

public interface IApiClient
{
    Task<ApiResponse<T>> GetAsync<T>(string url, CancellationToken ct = default);
    Task<ApiResponse<T>> PostAsync<T>(string url, object body, CancellationToken ct = default);
    Task<ApiResponse<T>> PutAsync<T>(string url, object body, CancellationToken ct = default);
    Task<ApiResponse<T>> DeleteAsync<T>(string url, CancellationToken ct = default);
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
}
