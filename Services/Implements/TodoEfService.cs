using Services.Interfaces;
using Shared.Entities.Dtos;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Services.Implements;

public class TodoEfService : ITodoEfService
{
    private readonly IStoredProcedureExecutor _sp;

    public TodoEfService(IStoredProcedureExecutor sp)
    {
        _sp = sp;
    }

    public async Task<IReadOnlyList<TodoItemDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var all = await _sp.QueryAsync<TodoItemDto>("dbo.usp_Todo_GetAll");
        return string.IsNullOrWhiteSpace(search)
            ? all
            : all.Where(x => x.Title.Contains(search!, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => _sp.QuerySingleAsync<TodoItemDto>("dbo.usp_Todo_GetById", new { Id = id });

    public async Task<(IReadOnlyList<TodoItemDto> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;
        var items = await _sp.QueryAsync<TodoItemDto>("dbo.usp_Todo_ListPaged", new { PageNumber = pageNumber, PageSize = pageSize, Search = search, TotalCount = 0 });
        var count = await _sp.QuerySingleAsync<CountDto>("dbo.usp_Todo_Count", new { Search = search });
        return (items, count?.TotalCount ?? items.Count);
    }

    public async Task<TodoItemDto> CreateAsync(string title, CancellationToken ct = default)
    {
        // Provide @Id output param manually
        var idParam = new SqlParameter("@Id", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var list = await _sp.QueryAsync<TodoItemDto>("dbo.usp_Todo_Create", new[]
        {
            new SqlParameter("@Title", title.Trim()),
            idParam
        });
        // SP returns the created row; list[0] should exist
        return list.First();
    }

    public Task<TodoItemDto?> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default)
        => _sp.QuerySingleAsync<TodoItemDto>("dbo.usp_Todo_Update", new { Id = id, Title = title.Trim(), IsDone = isDone });

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var affected = await _sp.QuerySingleAsync<AffectedDto>("dbo.usp_Todo_Delete", new { Id = id });
        return (affected?.Affected ?? 0) > 0;
    }

    private record CountDto(int TotalCount);
    private record AffectedDto(int Affected);
}
