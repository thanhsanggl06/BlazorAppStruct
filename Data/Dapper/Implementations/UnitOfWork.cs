using System.Data;
using Data.Dapper.Interfaces;

namespace Data.Dapper.Implementations;

/// <summary>
/// Unit of Work implementation cho Dapper
/// Qu?n l? connection và transaction lifecycle
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                _connection = _connectionFactory.CreateConnection();
            }
            return _connection;
        }
    }

    public IDbTransaction? Transaction => _transaction;

    public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction ð? ðý?c b?t ð?u.");
        }

        _transaction = Connection.BeginTransaction(isolationLevel);
    }

    public void Commit()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("Không có transaction nào ð? commit.");
        }

        try
        {
            _transaction.Commit();
        }
        catch
        {
            _transaction.Rollback();
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Rollback()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("Không có transaction nào ð? rollback.");
        }

        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Rollback transaction n?u chýa commit
                if (_transaction != null)
                {
                    _transaction.Rollback();
                    _transaction.Dispose();
                    _transaction = null;
                }

                // Ðóng connection
                if (_connection != null)
                {
                    _connection.Dispose();
                    _connection = null;
                }
            }

            _disposed = true;
        }
    }
}
