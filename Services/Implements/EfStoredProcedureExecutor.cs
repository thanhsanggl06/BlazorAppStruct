using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Data;
using Services.Interfaces;
using Services.Abstractions;

namespace Services.Implements;

public class EfStoredProcedureExecutor : IEfStoredProcedureExecutor
{
    private readonly AppDbContext _db;

    public EfStoredProcedureExecutor(AppDbContext db)
    {
        _db = db;
    }

    private DbConnection GetOpenConnection()
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();
        return conn;
    }

    private static void ApplyTimeout(DbCommand cmd, int? commandTimeout)
    {
        if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;
    }

    private static void ApplyParameters(DbCommand cmd, object? parameters)
    {
        if (parameters is null) return;

        if (parameters is IEnumerable<DbParameter> dbParams)
        {
            foreach (var p in dbParams) cmd.Parameters.Add(p);
            return;
        }

        if (parameters is IEnumerable<SpParameter> spParams)
        {
            foreach (var p in spParams)
            {
                var sqlParam = new SqlParameter(p.Name.StartsWith("@") ? p.Name : "@" + p.Name, p.Value ?? DBNull.Value);
                cmd.Parameters.Add(sqlParam);
            }
            return;
        }

        if (parameters is IDictionary<string, object?> dict)
        {
            foreach (var kv in dict)
            {
                var sqlParam = new SqlParameter(kv.Key.StartsWith("@") ? kv.Key : "@" + kv.Key, kv.Value ?? DBNull.Value);
                cmd.Parameters.Add(sqlParam);
            }
            return;
        }

        // anonymous object or POCO
        var props = parameters.GetType().GetProperties();
        foreach (var pi in props)
        {
            var name = pi.Name;
            var value = pi.GetValue(parameters, null);
            var sqlParam = new SqlParameter(name.StartsWith("@") ? name : "@" + name, value ?? DBNull.Value);
            cmd.Parameters.Add(sqlParam);
        }
    }

    public async Task<int> ExecuteAsync(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default)
    {
        await using var conn = GetOpenConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;
        ApplyTimeout(cmd, commandTimeout);
        ApplyParameters(cmd, parameters);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<T?> QuerySingleAsync<T>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default) where T : class, new()
    {
        var list = await QueryAsync<T>(spName, parameters, commandTimeout, ct);
        return list.FirstOrDefault();
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default) where T : class, new()
    {
        await using var conn = GetOpenConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;
        ApplyTimeout(cmd, commandTimeout);
        ApplyParameters(cmd, parameters);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new List<T>();
        var props = typeof(T).GetProperties().Where(p => p.CanWrite).ToArray();
        var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++) ordinals[reader.GetName(i)] = i;

        while (await reader.ReadAsync(ct))
        {
            var item = new T();
            foreach (var p in props)
            {
                if (!ordinals.TryGetValue(p.Name, out var ord)) continue;
                var val = reader.IsDBNull(ord) ? null : reader.GetValue(ord);
                if (val is null) continue;
                var targetType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                p.SetValue(item, Convert.ChangeType(val, targetType));
            }
            result.Add(item);
        }
        return result;
    }

    public async Task<(IReadOnlyList<TFirst> First, IReadOnlyList<TSecond> Second)> QueryMultipleAsync<TFirst, TSecond>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default) where TFirst : class, new() where TSecond : class, new()
    {
        await using var conn = GetOpenConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;
        ApplyTimeout(cmd, commandTimeout);
        ApplyParameters(cmd, parameters);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var first = await ReadListAsync<TFirst>(reader, ct);
        var second = new List<TSecond>();
        if (await reader.NextResultAsync(ct))
        {
            second = await ReadListAsync<TSecond>(reader, ct);
        }
        return (first, second);
    }

    private static async Task<List<T>> ReadListAsync<T>(DbDataReader reader, CancellationToken ct) where T : class, new()
    {
        var list = new List<T>();
        var props = typeof(T).GetProperties().Where(p => p.CanWrite).ToArray();
        var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++) ordinals[reader.GetName(i)] = i;

        while (await reader.ReadAsync(ct))
        {
            var item = new T();
            foreach (var p in props)
            {
                if (!ordinals.TryGetValue(p.Name, out var ord)) continue;
                var val = reader.IsDBNull(ord) ? null : reader.GetValue(ord);
                if (val is null) continue;
                var targetType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                p.SetValue(item, Convert.ChangeType(val, targetType));
            }
            list.Add(item);
        }
        return list;
    }
}
