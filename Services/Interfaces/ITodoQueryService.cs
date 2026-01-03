using Shared.Entities.Dtos;

namespace Services.Interfaces;

/// <summary>
/// Service interface cho query operations
/// Demo cách dùng Repository v?i/không transaction
/// </summary>
public interface ITodoQueryService
{
    /// <summary>
    /// Query ðõn gi?n không c?n transaction
    /// </summary>
    Task<TodoItemDto?> GetTodoByIdAsync(int id, CancellationToken ct = default);
    
    /// <summary>
    /// Get all todos không c?n transaction
    /// </summary>
    Task<IReadOnlyList<TodoItemDto>> GetAllTodosAsync(CancellationToken ct = default);
    
    /// <summary>
    /// T?o single todo không c?n transaction
    /// </summary>
    Task<int> CreateTodoAsync(string title, CancellationToken ct = default);
    
    /// <summary>
    /// Update single todo không c?n transaction
    /// </summary>
    Task<bool> UpdateTodoAsync(int id, string title, bool isDone, CancellationToken ct = default);
    
    /// <summary>
    /// Ð?c statistics trong transaction ð? ð?m b?o consistency
    /// </summary>
    Task<(IReadOnlyList<TodoItemDto> All, int CompletedCount)> GetTodoStatisticsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Query v?i Serializable isolation
    /// </summary>
    Task<IReadOnlyList<TodoItemDto>> GetTodosWithSerializableIsolationAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Approximate count v?i ReadUncommitted (dirty read)
    /// </summary>
    Task<int> GetApproximateTodoCountAsync(CancellationToken ct = default);
}
