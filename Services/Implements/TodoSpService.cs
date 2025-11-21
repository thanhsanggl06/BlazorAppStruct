using Services.Interfaces;
using Shared.Entities.Dtos;
using Microsoft.Data.SqlClient;

namespace Services.Implements;

public class TodoSpService : ITodoSpService
{
    private readonly IAdoStoredProcedureExecutor _sp;

    public TodoSpService(IAdoStoredProcedureExecutor sp)
    {
        _sp = sp;
    }

    public async Task<IReadOnlyList<TodoItemDto>> GetAllAsync(string? search, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Basic filtering via paged SP with large page size could be implemented; keep simple get all
            return await _sp.QueryAsync<TodoItemDto>("dbo.usp_Todo_GetAll", null, null, ct);
        }
        return await _sp.QueryAsync<TodoItemDto>("dbo.usp_Todo_GetAll", null, null, ct);
    }

    public Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => _sp.QuerySingleAsync<TodoItemDto>("dbo.usp_Todo_GetById", new { Id = id }, null, ct);

    public async Task<(IReadOnlyList<TodoItemDto> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize, string? search, CancellationToken ct = default)
    {
        var parameters = new List<SqlParameter>
        {
            new("@PageNumber", pageNumber),
            new("@PageSize", pageSize),
            new("@Search", (object?) (string.IsNullOrWhiteSpace(search) ? null : search) ?? DBNull.Value),
            new("@TotalCount", System.Data.SqlDbType.Int){ Direction = System.Data.ParameterDirection.Output }
        };

        var items = await _sp.QueryAsync<TodoItemDto>("dbo.usp_Todo_ListPaged", parameters, null, ct);
        var totalObj = parameters.Last().Value;
        var total = (totalObj == DBNull.Value || totalObj is null) ? items.Count : Convert.ToInt32(totalObj);
        return (items, total);
    }

    public async Task<TodoItemDto?> CreateAsync(string title, CancellationToken ct = default)
        => await _sp.QuerySingleAsync<TodoItemDto>("dbo.usp_Todo_Create", new { Title = title }, null, ct);

    public async Task<TodoItemDto?> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default)
        => await _sp.QuerySingleAsync<TodoItemDto>("dbo.usp_Todo_Update", new { Id = id, Title = title, IsDone = isDone }, null, ct);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var row = await _sp.QuerySingleAsync<AffectedDto>("dbo.usp_Todo_Delete", new { Id = id }, null, ct);
        return (row?.Affected ?? 0) > 0;
    }

    private record AffectedDto(int Affected);
}
