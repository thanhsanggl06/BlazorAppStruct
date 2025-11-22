using Shared.Entities.Dtos;
using Shared.Contracts;

namespace Client.Services;

public interface ITodoExtSpApi
{
    Task<ApiResponse<IReadOnlyList<TodoItemDto>>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<ApiResponse<PagedTodosDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> CreateAsync(string title, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default);
    Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default);
}

public class TodoExtSpApi : ITodoExtSpApi
{
    private readonly IApiClient _client;
    public TodoExtSpApi(IApiClient client) => _client = client;

    public Task<ApiResponse<IReadOnlyList<TodoItemDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(search) ? "api/todo-extsp" : $"api/todo-extsp?search={Uri.EscapeDataString(search)}";
        return _client.GetAsync<IReadOnlyList<TodoItemDto>>(url, ct);
    }

    public Task<ApiResponse<PagedTodosDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var url = $"api/todo-extsp/paged?pageNumber={pageNumber}&pageSize={pageSize}" + (string.IsNullOrWhiteSpace(search) ? string.Empty : $"&search={Uri.EscapeDataString(search)}");
        return _client.GetAsync<PagedTodosDto>(url, ct);
    }

    public Task<ApiResponse<TodoItemDto>> GetByIdAsync(int id, CancellationToken ct = default)
        => _client.GetAsync<TodoItemDto>($"api/todo-extsp/{id}", ct);

    public Task<ApiResponse<TodoItemDto>> CreateAsync(string title, CancellationToken ct = default)
        => _client.PostAsync<TodoItemDto>("api/todo-extsp", new CreateTodoRequest(title), ct);

    public Task<ApiResponse<TodoItemDto>> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default)
        => _client.PutAsync<TodoItemDto>($"api/todo-extsp/{id}", new UpdateTodoRequest(title, isDone), ct);

    public Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default)
        => _client.DeleteAsync<object>($"api/todo-extsp/{id}", ct);
}
