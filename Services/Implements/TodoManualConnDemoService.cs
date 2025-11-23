using Data;
using Microsoft.Data.SqlClient;
using Shared.Entities.Dtos;
using Services.Interfaces;
using System.Data;
using Microsoft.EntityFrameworkCore; // added for GetDbConnection extension

namespace Services.Implements;

/// <summary>
/// Demo service t? qu?n l? k?t n?i (manageConnection = false) khi g?i nhi?u stored procedure liên ti?p.
/// </summary>
public class TodoManualConnDemoService : ITodoManualConnDemoService
{
    private readonly AppDbContext _db;
    public TodoManualConnDemoService(AppDbContext db) => _db = db;

    public async Task<(IReadOnlyList<TodoItemDto> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;

        var conn = _db.Database.GetDbConnection();
        bool openedHere = false;
        if (conn.State == ConnectionState.Closed)
        {
            await conn.OpenAsync(ct);
            openedHere = true;
        }

        try
        {
            var items = new List<TodoItemDto>();
            var totalCount = 0;

            using (var cmdPaged = _db.LoadStoredProc("dbo.usp_Todo_ListPaged", prependDefaultSchema: false))
            {
                var totalParam = new SqlParameter("@TotalCount", SqlDbType.Int) { Direction = ParameterDirection.Output };
                cmdPaged
                    .WithSqlParam("@PageNumber", pageNumber)
                    .WithSqlParam("@PageSize", pageSize)
                    .WithSqlParam("@Search", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : search!)
                    .WithSqlParam(totalParam);

                await cmdPaged.ExecuteStoredProcAsync(r =>
                {
                    items = r.ReadToList<TodoItemDto>().ToList();
                }, manageConnection: false, ct: ct);

                totalCount = totalParam.Value is int i ? i : items.Count;
            }

            // Tùy ch?n g?i thêm SP count (minh h?a) dùng chung connection
            using (var cmdCount = _db.LoadStoredProc("dbo.usp_Todo_Count", prependDefaultSchema: false))
            {
                cmdCount.WithSqlParam("@Search", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : search!);
                await cmdCount.ExecuteStoredProcAsync(r =>
                {
                    var countRow = r.ReadToList<CountDto>().FirstOrDefault();
                    if (countRow is not null) totalCount = countRow.TotalCount;
                }, manageConnection: false, ct: ct);
            }

            return (items, totalCount);
        }
        finally
        {
            if (openedHere && conn.State != ConnectionState.Closed)
                conn.Close();
        }
    }

    private class CountDto { public int TotalCount { get; set; } }
}
