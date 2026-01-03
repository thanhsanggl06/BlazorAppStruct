using System.Data;

namespace Data.Dapper.Interfaces;

/// <summary>
/// Unit of Work pattern cho Dapper - qu?n l? transaction xuyên su?t nhi?u repository operations
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Database connection ðý?c s? d?ng trong unit of work
    /// </summary>
    IDbConnection Connection { get; }
    
    /// <summary>
    /// Transaction hi?n t?i (null n?u chýa begin)
    /// </summary>
    IDbTransaction? Transaction { get; }
    
    /// <summary>
    /// B?t ð?u transaction
    /// </summary>
    void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
    
    /// <summary>
    /// Commit transaction
    /// </summary>
    void Commit();
    
    /// <summary>
    /// Rollback transaction
    /// </summary>
    void Rollback();
}
