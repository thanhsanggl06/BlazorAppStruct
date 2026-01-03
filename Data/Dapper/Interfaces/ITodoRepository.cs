using Shared.Entities.Dtos;
using System.Data;

namespace Data.Dapper.Interfaces;

/// <summary>
/// Repository interface cho Todo operations v?i Dapper
/// M?i method cho phép override transaction, m?c ð?nh dùng UnitOfWork.Transaction
/// </summary>
public interface ITodoRepository
{
    Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default, IDbTransaction? transaction = null);
    
    Task<IReadOnlyList<TodoItemDto>> GetAllAsync(CancellationToken ct = default, IDbTransaction? transaction = null);
    
    Task<(IReadOnlyList<TodoItemDto> Items, int Total)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        string? search = null, 
        CancellationToken ct = default,
        IDbTransaction? transaction = null);
    
    Task<int> CreateAsync(string title, CancellationToken ct = default, IDbTransaction? transaction = null);
    
    Task<bool> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default, IDbTransaction? transaction = null);
    
    Task<bool> DeleteAsync(int id, CancellationToken ct = default, IDbTransaction? transaction = null);
    
    Task<int> CountAsync(string? search = null, CancellationToken ct = default, IDbTransaction? transaction = null);
}
