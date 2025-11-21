using System.Data;

namespace Services.Interfaces;

public interface IEfStoredProcedureExecutor
{
    Task<int> ExecuteAsync(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default);
    Task<T?> QuerySingleAsync<T>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default) where T : class, new();
    Task<IReadOnlyList<T>> QueryAsync<T>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default) where T : class, new();
    Task<(IReadOnlyList<TFirst> First, IReadOnlyList<TSecond> Second)> QueryMultipleAsync<TFirst, TSecond>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default)
        where TFirst : class, new()
        where TSecond : class, new();
}
