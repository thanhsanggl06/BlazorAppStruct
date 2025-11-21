using Shared.Entities.Dtos;
using Shared.Contracts;

namespace Client.Services;

public interface ITodoSpApi
{
    Task<ApiResponse<IReadOnlyList<TodoItemDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResponse<object>> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> CreateAsync(string title, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default);
    Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default);
}

public class TodoSpApi : ITodoSpApi
{
    private readonly IApiClient _client;
    public TodoSpApi(IApiClient client) => _client = client;

    public Task<ApiResponse<IReadOnlyList<TodoItemDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(search) ? "api/todo-sp" : $"api/todo-sp?search={Uri.EscapeDataString(search)}";
        return _client.GetAsync<IReadOnlyList<TodoItemDto>>(url, ct);
    }

    public Task<ApiResponse<TodoItemDto>> GetByIdAsync(int id, CancellationToken ct = default)
        => _client.GetAsync<TodoItemDto>($"api/todo-sp/{id}", ct);

    public Task<ApiResponse<object>> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var url = $"api/todo-sp/paged?pageNumber={pageNumber}&pageSize={pageSize}" + (string.IsNullOrWhiteSpace(search) ? string.Empty : $"&search={Uri.EscapeDataString(search)}");
        return _client.GetAsync<object>(url, ct);
    }

    public Task<ApiResponse<TodoItemDto>> CreateAsync(string title, CancellationToken ct = default)
        => _client.PostAsync<TodoItemDto>("api/todo-sp", new CreateTodoRequest(title), ct);

    public Task<ApiResponse<TodoItemDto>> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default)
        => _client.PutAsync<TodoItemDto>($"api/todo-sp/{id}", new UpdateTodoRequest(title, isDone), ct);

    public Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default)
        => _client.DeleteAsync<object>($"api/todo-sp/{id}", ct);
}
