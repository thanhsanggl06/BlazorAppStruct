using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Shared.Entities.Dtos;

namespace Services.Implements;

public class TodoDapperService : ITodoDapperService
{
    private readonly string _connectionString;

    public TodoDapperService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new System.InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<IEnumerable<TodoItemDto>> GetAllAsync()
    {
        const string sql = "SELECT Id, Title, IsDone FROM TodoItems ORDER BY Id";
        using var conn = CreateConnection();
        return await conn.QueryAsync<TodoItemDto>(sql);
    }

    public async Task<TodoItemDto?> GetByIdAsync(int id)
    {
        const string sql = "SELECT Id, Title, IsDone FROM TodoItems WHERE Id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<TodoItemDto>(sql, new { Id = id });
    }
}
