using Shared.Entities.Dtos;

namespace Services.Interfaces;

public interface ITodoEfService
{
    Task<IReadOnlyList<TodoItemDto>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IReadOnlyList<TodoItemDto> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default);
    Task<TodoItemDto> CreateAsync(string title, CancellationToken ct = default);
    Task<TodoItemDto?> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
