using Data;
using Microsoft.Data.SqlClient;
using Services.Interfaces;
using Shared.Entities.Dtos;
using System.Data;

namespace Services.Implements;

public class TodoExtSpService : ITodoExtSpService
{
    private readonly AppDbContext _db;

    public TodoExtSpService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TodoItemDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var items = new List<TodoItemDto>();
        using var cmd = _db.LoadStoredProc("dbo.usp_Todo_GetAll");
        await cmd.ExecuteStoredProcAsync(r =>
        {
            items = r.ReadToList<TodoItemDto>().ToList();
        }, manageConnection: true, ct: ct);
        // simple client-side filtering if search provided
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(x => x.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        return items;
    }

    public async Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var list = new List<TodoItemDto>();
        using var cmd = _db.LoadStoredProc("dbo.usp_Todo_GetById")
            .WithSqlParam("@Id", id);
        await cmd.ExecuteStoredProcAsync(r =>
        {
            list = r.ReadToList<TodoItemDto>().ToList();
        }, manageConnection: true, ct: ct);
        return list.FirstOrDefault();
    }

    public async Task<(IReadOnlyList<TodoItemDto> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;

        var totalParam = new SqlParameter("@TotalCount", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var items = new List<TodoItemDto>();
        using var cmd = _db.LoadStoredProc("dbo.usp_Todo_ListPaged")
            .WithSqlParam("@PageNumber", pageNumber)
            .WithSqlParam("@PageSize", pageSize)
            .WithSqlParam("@Search", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : search!)
            .WithSqlParam(totalParam);

        await cmd.ExecuteStoredProcAsync(r =>
        {
            items = r.ReadToList<TodoItemDto>().ToList();
        }, manageConnection: true, ct: ct);

        var total = totalParam.Value is null or DBNull ? items.Count : Convert.ToInt32(totalParam.Value);
        return (items, total);
    }

    public async Task<TodoItemDto?> CreateAsync(string title, CancellationToken ct = default)
    {
        var idParam = new SqlParameter("@Id", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var list = new List<TodoItemDto>();
        using var cmd = _db.LoadStoredProc("dbo.usp_Todo_Create")
            .WithSqlParam("@Title", title.Trim())
            .WithSqlParam(idParam);
        await cmd.ExecuteStoredProcAsync(r =>
        {
            list = r.ReadToList<TodoItemDto>().ToList();
        }, manageConnection: true, ct: ct);
        return list.FirstOrDefault();
    }

    public async Task<TodoItemDto?> UpdateAsync(int id, string title, bool isDone, CancellationToken ct = default)
    {
        var list = new List<TodoItemDto>();
        using var cmd = _db.LoadStoredProc("dbo.usp_Todo_Update")
            .WithSqlParam("@Id", id)
            .WithSqlParam("@Title", title.Trim())
            .WithSqlParam("@IsDone", isDone);
        await cmd.ExecuteStoredProcAsync(r =>
        {
            list = r.ReadToList<TodoItemDto>().ToList();
        }, manageConnection: true, ct: ct);
        return list.FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var rows = new List<AffectedDto>();
        using var cmd = _db.LoadStoredProc("dbo.usp_Todo_Delete")
            .WithSqlParam("@Id", id);
        await cmd.ExecuteStoredProcAsync(r =>
        {
            rows = r.ReadToList<AffectedDto>().ToList();
        }, manageConnection: true, ct: ct);
        var affectedRows = rows.FirstOrDefault()?.Affected ?? 0;
        return affectedRows > 0;
    }

    private class AffectedDto
    {
        public int Affected { get; set; }
    }
}
