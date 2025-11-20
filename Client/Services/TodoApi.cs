using Shared.Entities.Dtos;
using Shared.Contracts;

namespace Client.Services;

public interface ITodoApi
{
    Task<ApiResponse<IReadOnlyList<TodoItemDto>>> GetAllAsync(CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> CreateAsync(CreateTodoRequest request, CancellationToken ct = default);
    Task<ApiResponse<TodoItemDto>> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken ct = default);
    Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default);
}

public class TodoApi : ITodoApi
{
    private readonly IApiClient _client;
    public TodoApi(IApiClient client) => _client = client;

    public Task<ApiResponse<IReadOnlyList<TodoItemDto>>> GetAllAsync(CancellationToken ct = default)
        => _client.GetAsync<IReadOnlyList<TodoItemDto>>("api/todos", ct);

    public Task<ApiResponse<TodoItemDto>> GetByIdAsync(int id, CancellationToken ct = default)
        => _client.GetAsync<TodoItemDto>($"api/todos/{id}", ct);

    public Task<ApiResponse<TodoItemDto>> CreateAsync(CreateTodoRequest request, CancellationToken ct = default)
        => _client.PostAsync<TodoItemDto>("api/todos", request, ct);

    public Task<ApiResponse<TodoItemDto>> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken ct = default)
        => _client.PutAsync<TodoItemDto>($"api/todos/{id}", request, ct);

    public Task<ApiResponse<object>> DeleteAsync(int id, CancellationToken ct = default)
        => _client.DeleteAsync<object>($"api/todos/{id}", ct);
}
