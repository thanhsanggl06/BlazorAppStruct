using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Services.Implements;

public class AdoStoredProcedureExecutor : IAdoStoredProcedureExecutor
{
    private readonly string _connectionString;

    public AdoStoredProcedureExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
    }

    private async Task<SqlConnection> GetOpenConnectionAsync()
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
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

        if (parameters is IDictionary<string, object?> dict)
        {
            foreach (var kv in dict)
            {
                var sqlParam = new SqlParameter(kv.Key.StartsWith("@") ? kv.Key : "@" + kv.Key, kv.Value ?? DBNull.Value);
                cmd.Parameters.Add(sqlParam);
            }
            return;
        }

        var props = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
        await using var conn = await GetOpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;
        ApplyTimeout(cmd, commandTimeout);
        ApplyParameters(cmd, parameters);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<T?> QuerySingleAsync<T>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default)
    {
        var list = await QueryAsync<T>(spName, parameters, commandTimeout, ct);
        return list.FirstOrDefault();
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default)
    {
        await using var conn = await GetOpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;
        ApplyTimeout(cmd, commandTimeout);
        ApplyParameters(cmd, parameters);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var ordinals = BuildOrdinals(reader);

        var type = typeof(T);
        var ctor = SelectPrimaryConstructor(type);
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite).ToArray();

        var result = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(MapRow<T>(reader, ordinals, ctor, props));
        }
        return result;
    }

    public async Task<(IReadOnlyList<TFirst> First, IReadOnlyList<TSecond> Second)> QueryMultipleAsync<TFirst, TSecond>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default)
    {
        await using var conn = await GetOpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;
        ApplyTimeout(cmd, commandTimeout);
        ApplyParameters(cmd, parameters);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var first = await ReadListAsync<TFirst>(reader, ct);
        var second = new List<TSecond>();
        if (await reader.NextResultAsync(ct))
            second = await ReadListAsync<TSecond>(reader, ct);
        return (first, second);
    }

    private static async Task<List<T>> ReadListAsync<T>(DbDataReader reader, CancellationToken ct)
    {
        var ordinals = BuildOrdinals(reader);
        var type = typeof(T);
        var ctor = SelectPrimaryConstructor(type);
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite).ToArray();
        var list = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapRow<T>(reader, ordinals, ctor, props));
        }
        return list;
    }

    private static Dictionary<string, int> BuildOrdinals(DbDataReader reader)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
            dict[reader.GetName(i)] = i;
        return dict;
    }

    private static ConstructorInfo? SelectPrimaryConstructor(Type type) => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
        .OrderByDescending(c => c.GetParameters().Length)
        .FirstOrDefault();

    private static T MapRow<T>(DbDataReader reader, Dictionary<string, int> ordinals, ConstructorInfo? ctor, PropertyInfo[] props)
    {
        if (ctor is not null && ctor.GetParameters().Length > 0)
        {
            var ctorParams = ctor.GetParameters();
            var args = new object?[ctorParams.Length];
            bool allMatched = true;
            for (int i = 0; i < ctorParams.Length; i++)
            {
                var p = ctorParams[i];
                var colNameCandidates = new[] { p.Name!, p.Name!.Replace("_", "") };
                var matchedName = colNameCandidates.FirstOrDefault(n => ordinals.ContainsKey(n));
                if (matchedName is null)
                {
                    allMatched = false;
                    break;
                }
                var ord = ordinals[matchedName];
                args[i] = reader.IsDBNull(ord) ? GetDefault(p.ParameterType) : ConvertTo(reader.GetValue(ord), p.ParameterType);
            }
            if (allMatched)
            {
                return (T)Activator.CreateInstance(typeof(T), args)!;
            }
            // else fallback to property mapping
        }
        var instance = Activator.CreateInstance<T>();
        foreach (var prop in props)
        {
            var candidates = new[] { prop.Name, prop.Name.Replace("_", "") };
            var match = candidates.FirstOrDefault(n => ordinals.ContainsKey(n));
            if (match is null) continue;
            var ord = ordinals[match];
            if (reader.IsDBNull(ord)) continue;
            prop.SetValue(instance, ConvertTo(reader.GetValue(ord), prop.PropertyType));
        }
        return instance;
    }

    private static object? GetDefault(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

    private static object? ConvertTo(object value, Type targetType)
    {
        if (value is null) return null;
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying.IsEnum) return Enum.ToObject(underlying, value);
        return Convert.ChangeType(value, underlying);
    }
}
