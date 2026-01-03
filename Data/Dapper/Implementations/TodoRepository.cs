using System.Data;
using Dapper;
using Data.Dapper.Interfaces;
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
        
        // Dùng transaction ðý?c truy?n vào, n?u null th? dùng c?a UnitOfWork
        var txn = transaction ?? _unitOfWork.Transaction;
        
        var result = await _unitOfWork.Connection.QueryFirstOrDefaultAsync<TodoItemDto>(
            new CommandDefinition(sql, new { Id = id }, txn, cancellationToken: ct)
        );
        
        return result;
    }

    public async Task<IReadOnlyList<TodoItemDto>> GetAllAsync(CancellationToken ct = default, IDbTransaction? transaction = null)
    {
        const string sql = "SELECT Id, Title, IsDone, CreatedAt FROM TodoItems ORDER BY CreatedAt DESC";
        
        var txn = transaction ?? _unitOfWork.Transaction;
        
        var results = await _unitOfWork.Connection.QueryAsync<TodoItemDto>(
            new CommandDefinition(sql, transaction: txn, cancellationToken: ct)
        );
        
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

        var items = await _unitOfWork.Connection.QueryAsync<TodoItemDto>(
            new CommandDefinition(
                "dbo.usp_Todo_ListPaged",
                parameters,
                txn,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct
            )
        );

        var total = parameters.Get<int>("@TotalCount");

        return (items.ToList(), total);
    }

    public async Task<int> CreateAsync(string title, CancellationToken ct = default, IDbTransaction? transaction = null)
    {
        const string sql = @"
            INSERT INTO TodoItems (Title, IsDone, CreatedAt)
            VALUES (@Title, 0, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var txn = transaction ?? _unitOfWork.Transaction;

        var newId = await _unitOfWork.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Title = title }, txn, cancellationToken: ct)
        );
        
        return newId;
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
            new CommandDefinition(
                sql,
                new { Id = id, Title = title, IsDone = isDone },
                txn,
                cancellationToken: ct
            )
        );
        
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default, IDbTransaction? transaction = null)
    {
        const string sql = "DELETE FROM TodoItems WHERE Id = @Id";

        var txn = transaction ?? _unitOfWork.Transaction;

        var affectedRows = await _unitOfWork.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, txn, cancellationToken: ct)
        );
        
        return affectedRows > 0;
    }

    public async Task<int> CountAsync(string? search = null, CancellationToken ct = default, IDbTransaction? transaction = null)
    {
        string sql;
        object param;

        if (string.IsNullOrWhiteSpace(search))
        {
            sql = "SELECT COUNT(*) FROM TodoItems";
            param = new { };
        }
        else
        {
            sql = "SELECT COUNT(*) FROM TodoItems WHERE Title LIKE @Search";
            param = new { Search = $"%{search}%" };
        }

        var txn = transaction ?? _unitOfWork.Transaction;

        var count = await _unitOfWork.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, param, txn, cancellationToken: ct)
        );
        
        return count;
    }
}
