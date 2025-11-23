using Data;
using Microsoft.Data.SqlClient;
using Shared.Entities.Dtos;
using System.Data;

namespace Services.Interfaces;

public interface ITodoManualConnDemoService
{
    Task<(IReadOnlyList<TodoItemDto> Items, int Total)> GetPagedAsync(int pageNumber, int pageSize, string? search = null, CancellationToken ct = default);
}
