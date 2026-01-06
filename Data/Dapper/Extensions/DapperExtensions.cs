using System.Data;
using Dapper;

namespace Data.Dapper.Extensions;

/// <summary>
/// Extension methods cho IDbConnection ð? vi?t Dapper code ng?n g?n hõn
/// T? ð?ng inject transaction và cancellationToken
/// </summary>
public static class DapperExtensions
{
    /// <summary>
    /// Query single result v?i auto-inject transaction
    /// </summary>
    public static async Task<T?> QuerySingleAsync<T>(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        return await connection.QueryFirstOrDefaultAsync<T>(
            new CommandDefinition(sql, param, transaction, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Query multiple results v?i auto-inject transaction
    /// </summary>
    public static async Task<IEnumerable<T>> QueryListAsync<T>(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        return await connection.QueryAsync<T>(
            new CommandDefinition(sql, param, transaction, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Execute command (INSERT/UPDATE/DELETE) v?i auto-inject transaction
    /// </summary>
    public static async Task<int> ExecuteAsync(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        return await connection.ExecuteAsync(
            new CommandDefinition(sql, param, transaction, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Execute scalar (SELECT COUNT, SELECT SCOPE_IDENTITY) v?i auto-inject transaction
    /// </summary>
    public static async Task<T> ExecuteScalarAsync<T>(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        return await connection.ExecuteScalarAsync<T>(
            new CommandDefinition(sql, param, transaction, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Execute stored procedure v?i auto-inject transaction
    /// </summary>
    public static async Task<IEnumerable<T>> ExecuteStoredProcAsync<T>(
        this IDbConnection connection,
        string procedureName,
        object? param = null,
        IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        return await connection.QueryAsync<T>(
            new CommandDefinition(
                procedureName,
                param,
                transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct
            )
        );
    }

    /// <summary>
    /// Execute stored procedure v?i output parameters
    /// </summary>
    public static async Task<(IEnumerable<T> Results, DynamicParameters Parameters)> ExecuteStoredProcWithOutputAsync<T>(
        this IDbConnection connection,
        string procedureName,
        DynamicParameters parameters,
        IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        var results = await connection.QueryAsync<T>(
            new CommandDefinition(
                procedureName,
                parameters,
                transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct
            )
        );

        return (results, parameters);
    }
}
