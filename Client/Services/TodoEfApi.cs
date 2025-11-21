using Shared.Entities.Dtos;
using Shared.Contracts;

namespace Client.Services;

public interface ITodoEfApi
{
    Task<ApiResponse<IReadOnlyList<TodoItemDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<ApiResponse<PagedTodosDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> CreateAsync(string title, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default);
    Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default);
}

public class TodoEfApi : ITodoEfApi
{
    private readonly IApiClient _client;
    public TodoEfApi(IApiClient client) => _client = client;

    public Task<ApiResponse<IReadOnlyList<TodoItemDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(search) ? "api/todo-ef" : $"api/todo-ef?search={Uri.EscapeDataString(search)}";
        return _client.GetAsync<IReadOnlyList<TodoItemDto>>(url, ct);
    }

    public Task<ApiResponse<PagedTodosDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var url = $"api/todo-ef/paged?pageNumber={pageNumber}&pageSize={pageSize}" + (string.IsNullOrWhiteSpace(search) ? string.Empty : $"&search={Uri.EscapeDataString(search)}");
        return _client.GetAsync<PagedTodosDto>(url, ct);
    }

    public Task<ApiResponse<TodoItemDto>> GetByIdAsync(int id, CancellationToken ct = default)
        => _client.GetAsync<TodoItemDto>($"api/todo-ef/{id}", ct);

    public Task<ApiResponse<TodoItemDto>> CreateAsync(string title, CancellationToken ct = default)
        => _client.PostAsync<TodoItemDto>("api/todo-ef", new CreateTodoRequest(title), ct);

    public Task<ApiResponse<TodoItemDto>> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default)
        => _client.PutAsync<TodoItemDto>($"api/todo-ef/{id}", new UpdateTodoRequest(title, isDone), ct);

    public Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default)
        => _client.DeleteAsync<object>($"api/todo-ef/{id}", ct);
}
