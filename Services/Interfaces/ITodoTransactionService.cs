using Shared.Entities.Dtos;

namespace Services.Interfaces;

/// <summary>
/// Service interface cho Todo operations v?i Dapper UnitOfWork pattern
/// </summary>
public interface ITodoTransactionService
{
    /// <summary>
    /// T?o nhi?u todos trong m?t transaction
    /// </summary>
    Task<IReadOnlyList<int>> CreateMultipleTodosAsync(IEnumerable<string> titles, CancellationToken ct = default);
    
    /// <summary>
    /// C?p nh?t nhi?u todos và t?o m?i m?t todo trong cùng transaction
    /// Ví d?: Mark nhi?u todos là done và t?o todo t?ng k?t
    /// </summary>
    Task<(int UpdatedCount, int NewTodoId)> UpdateMultipleAndCreateSummaryAsync(
        IEnumerable<int> idsToComplete, 
        string summaryTitle, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Di chuy?n todos t? m?t danh sách sang danh sách khác (ví d?: archive)
    /// Bao g?m: ðánh d?u done, t?o b?n sao trong b?ng archive, xóa b?n g?c
    /// </summary>
    Task<int> ArchiveCompletedTodosAsync(IEnumerable<int> todoIds, CancellationToken ct = default);
    
    /// <summary>
    /// Bulk delete v?i validation - xóa nhi?u todos nhýng rollback n?u có l?i
    /// </summary>
    Task<int> BulkDeleteWithValidationAsync(IEnumerable<int> todoIds, CancellationToken ct = default);
    
    /// <summary>
    /// Complex transaction: Clone m?t todo, update b?n g?c, t?o log entry
    /// Ví d? th?c t?: t?o phiên b?n m?i c?a todo, ðánh d?u b?n c? là archived
    /// </summary>
    Task<int> CloneAndArchiveAsync(int todoId, string newTitle, CancellationToken ct = default);
}
