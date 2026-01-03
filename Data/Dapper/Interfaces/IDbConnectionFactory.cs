using System.Data;

namespace Data.Dapper.Interfaces;

/// <summary>
/// Factory ð? t?o database connection cho Dapper
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// T?o m?t connection m?i (caller ph?i dispose)
    /// </summary>
    IDbConnection CreateConnection();
}
