using System.Data.Common;

namespace Services.Interfaces;

public interface IAdoStoredProcedureExecutor
{
    Task<int> ExecuteAsync(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default);
    Task<T?> QuerySingleAsync<T>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default);
    Task<IReadOnlyList<T>> QueryAsync<T>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default);
    Task<(IReadOnlyList<TFirst> First, IReadOnlyList<TSecond> Second)> QueryMultipleAsync<TFirst, TSecond>(string spName, object? parameters = null, int? commandTimeout = null, CancellationToken ct = default);
}
