using System.Data;

namespace Services.Interfaces;

public interface IStoredProcedureExecutor
{
    Task<List<T>> QueryAsync<T>(string spName, object? parameters = null) where T : class;
    Task<T?> QuerySingleAsync<T>(string spName, object? parameters = null) where T : class;
    Task<int> ExecuteAsync(string spName, object? parameters = null);
}
