using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using System.Reflection;
using Data; // for AppDbContext
using System.Data.Common; // add

namespace Services.Implements;

public class StoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly AppDbContext _db;

    public StoredProcedureExecutor(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<T>> QueryAsync<T>(string spName, object? parameters = null) where T : class
    {
        var (sql, sqlParams) = BuildSql(spName, parameters);
        var isEntity = _db.Model.FindEntityType(typeof(T)) is not null && typeof(T) != typeof(object);
        if (isEntity)
        {
            return await _db.Set<T>().FromSqlRaw(sql, sqlParams).ToListAsync();
        }
        return await QueryViaAdoAsync<T>(sql, sqlParams);
    }

    public async Task<T?> QuerySingleAsync<T>(string spName, object? parameters = null) where T : class
    {
        var list = await QueryAsync<T>(spName, parameters);
        return list.FirstOrDefault();
    }

    public async Task<int> ExecuteAsync(string spName, object? parameters = null)
    {
        var (sql, sqlParams) = BuildSql(spName, parameters);
        return await _db.Database.ExecuteSqlRawAsync(sql, sqlParams);
    }

    private static (string Sql, object[] SqlParams) BuildSql(string spName, object? parameters)
    {
        if (parameters is null) return ($"EXEC {spName}", Array.Empty<object>());
        var paramList = new List<object>();
        var paramStrings = new List<string>();

        if (parameters is IEnumerable<DbParameter> dbParams)
        {
            foreach (var p in dbParams)
            {
                var name = p.ParameterName.StartsWith("@") ? p.ParameterName : "@" + p.ParameterName;
                paramStrings.Add(name);
                paramList.Add(p);
            }
            var sqlBuilt = $"EXEC {spName} {string.Join(", ", paramStrings)}";
            return (sqlBuilt, paramList.ToArray());
        }

        IEnumerable<(string Name, object? Value)> pairs;
        if (parameters is IDictionary<string, object?> dict)
            pairs = dict.Select(kv => (kv.Key, kv.Value));
        else
        {
            var props = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            pairs = props.Select(p => (p.Name, p.GetValue(parameters) ?? DBNull.Value));
        }

        foreach (var (name, value) in pairs)
        {
            var paramName = name.StartsWith("@") ? name : "@" + name;
            paramStrings.Add(paramName);
            paramList.Add(new SqlParameter(paramName, value ?? DBNull.Value));
        }

        var sql = $"EXEC {spName} {string.Join(", ", paramStrings)}";
        return (sql, paramList.ToArray());
    }

    private async Task<List<T>> QueryViaAdoAsync<T>(string sql, object[] sqlParams) where T : class
    {
        await using var conn = new SqlConnection(_db.Database.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var p in sqlParams) cmd.Parameters.Add(p);
        await using var reader = await cmd.ExecuteReaderAsync();

        var list = new List<T>();
        var type = typeof(T);
        var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
        var writableProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++) ordinals[reader.GetName(i)] = i;

        while (await reader.ReadAsync())
        {
            T obj;
            if (ctor is not null && ctor.GetParameters().Length > 0)
            {
                var ctorParams = ctor.GetParameters();
                var args = new object?[ctorParams.Length];
                bool allMatched = true;
                for (int i = 0; i < ctorParams.Length; i++)
                {
                    var pInfo = ctorParams[i];
                    var candidates = new[] { pInfo.Name!, pInfo.Name!.Replace("_", "") };
                    var match = candidates.FirstOrDefault(c => ordinals.ContainsKey(c));
                    if (match is null)
                    {
                        allMatched = false;
                        break;
                    }
                    var ord = ordinals[match];
                    args[i] = reader.IsDBNull(ord) ? GetDefault(pInfo.ParameterType) : ConvertTo(reader.GetValue(ord), pInfo.ParameterType);
                }
                if (allMatched)
                {
                    obj = (T)Activator.CreateInstance(type, args)!;
                    list.Add(obj);
                    continue;
                }
            }
            obj = Activator.CreateInstance<T>();
            foreach (var prop in writableProps)
            {
                if (!ordinals.TryGetValue(prop.Name, out var ord)) continue;
                if (reader.IsDBNull(ord)) continue;
                var val = reader.GetValue(ord);
                prop.SetValue(obj, ConvertTo(val, prop.PropertyType));
            }
            list.Add(obj);
        }
        return list;
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
