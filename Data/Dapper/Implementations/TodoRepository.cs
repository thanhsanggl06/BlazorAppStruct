using System.Data;
using Dapper;
using Data.Dapper.Interfaces;
using Data.Dapper.Extensions;
using Shared.Entities.Dtos;

namespace Data.Dapper.Implementations;

/// <summary>
/// TodoRepository implementation v?i Dapper
/// LUÔN dùng UnitOfWork ð? qu?n l? connection
/// Cho phép override transaction n?u c?n
/// </summary>
public class TodoRepository : ITodoRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public TodoRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default, IDbTransaction? transaction = null)
    {
        const string sql = "SELECT Id, Title, IsDone, CreatedAt FROM TodoItems WHERE Id = @Id";
        
        var txn = transaction ?? _unitOfWork.Transaction;
        
        return await _unitOfWork.Connection.QuerySingleAsync<TodoItemDto>(
            sql, 
            new { Id = id }, 
            txn, 
            ct
        );
    }

    public async Task<IReadOnlyList<TodoItemDto>> GetAllAsync(CancellationToken ct = default, IDbTransaction? transaction = null)
    {
        const string sql = "SELECT Id, Title, IsDone, CreatedAt FROM TodoItems ORDER BY CreatedAt DESC";
        
        var txn = transaction ?? _unitOfWork.Transaction;
        
        var results = await _unitOfWork.Connection.QueryListAsync<TodoItemDto>(sql, null, txn, ct);
        return results.ToList();
    }

    public async Task<(IReadOnlyList<TodoItemDto> Items, int Total)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        string? search = null, 
        CancellationToken ct = default,
        IDbTransaction? transaction = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;

        var txn = transaction ?? _unitOfWork.Transaction;

        var parameters = new DynamicParameters();
        parameters.Add("@PageNumber", pageNumber);
        parameters.Add("@PageSize", pageSize);
        parameters.Add("@Search", string.IsNullOrWhiteSpace(search) ? null : search);
        parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var (results, outParams) = await _unitOfWork.Connection.ExecuteStoredProcWithOutputAsync<TodoItemDto>(
            "dbo.usp_Todo_ListPaged",
            parameters,
            txn,
            ct
        );

        var total = outParams.Get<int>("@TotalCount");

        return (results.ToList(), total);
    }

    public async Task<int> CreateAsync(string title, CancellationToken ct = default, IDbTransaction? transaction = null)
    {
        const string sql = @"
            INSERT INTO TodoItems (Title, IsDone, CreatedAt)
            VALUES (@Title, 0, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var txn = transaction ?? _unitOfWork.Transaction;

        return await _unitOfWork.Connection.ExecuteScalarAsync<int>(
            sql, 
            new { Title = title }, 
            txn, 
            ct
        );
    }

    public async Task<bool> UpdateAsync(
        int id, 
        string title, 
        bool isDone, 
        CancellationToken ct = default,
        IDbTransaction? transaction = null)
    {
        const string sql = @"
            UPDATE TodoItems
            SET Title = @Title, IsDone = @IsDone
            WHERE Id = @Id";

        var txn = transaction ?? _unitOfWork.Transaction;

        var affectedRows = await _unitOfWork.Connection.ExecuteAsync(
            sql,
            new { Id = id, Title = title, IsDone = isDone },
            txn,
            ct
        );
        
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default, IDbTransaction? transaction = null)
    {
        const string sql = "DELETE FROM TodoItems WHERE Id = @Id";

        var txn = transaction ?? _unitOfWork.Transaction;

        var affectedRows = await _unitOfWork.Connection.ExecuteAsync(
            sql, 
            new { Id = id }, 
            txn, 
            ct
        );
        
        return affectedRows > 0;
    }

    public async Task<int> CountAsync(string? search = null, CancellationToken ct = default, IDbTransaction? transaction = null)
    {
        string sql;
        object? param;

        if (string.IsNullOrWhiteSpace(search))
        {
            sql = "SELECT COUNT(*) FROM TodoItems";
            param = null;
        }
        else
        {
            sql = "SELECT COUNT(*) FROM TodoItems WHERE Title LIKE @Search";
            param = new { Search = $"%{search}%" };
        }

        var txn = transaction ?? _unitOfWork.Transaction;

        return await _unitOfWork.Connection.ExecuteScalarAsync<int>(sql, param, txn, ct);
    }
}
