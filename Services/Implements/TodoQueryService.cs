using Data.Dapper.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Shared.Entities.Dtos;

namespace Services.Implements;

/// <summary>
/// Service demo cách s? d?ng Repository v?i UnitOfWork
/// - Có transaction: G?i BeginTransaction() trý?c khi dùng Repository
/// - Không transaction: Dùng Repository tr?c ti?p (auto-commit m?i operation)
/// </summary>
public class TodoQueryService : ITodoQueryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoRepository _repository;
    private readonly ILogger<TodoQueryService> _logger;

    public TodoQueryService(
        IUnitOfWork unitOfWork,
        ITodoRepository repository,
        ILogger<TodoQueryService> logger)
    {
        _unitOfWork = unitOfWork;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Query ðõn gi?n KHÔNG C?N transaction
    /// M?i repository operation t? commit
    /// </summary>
    public async Task<TodoItemDto?> GetTodoByIdAsync(int id, CancellationToken ct = default)
    {
        // KHÔNG g?i BeginTransaction() ? m?i query t? commit
        _logger.LogInformation("Getting todo {Id} without transaction", id);
        
        return await _repository.GetByIdAsync(id, ct);
        // Connection t? ð?ng commit sau query
    }

    /// <summary>
    /// Query list KHÔNG C?N transaction
    /// </summary>
    public async Task<IReadOnlyList<TodoItemDto>> GetAllTodosAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Getting all todos without transaction");
        
        return await _repository.GetAllAsync(ct);
    }

    /// <summary>
    /// T?o single todo KHÔNG C?N transaction
    /// </summary>
    public async Task<int> CreateTodoAsync(string title, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating single todo: {Title} without transaction", title);
        
        return await _repository.CreateAsync(title, ct);
        // Auto-commit sau insert
    }

    /// <summary>
    /// Update single todo KHÔNG C?N transaction
    /// </summary>
    public async Task<bool> UpdateTodoAsync(int id, string title, bool isDone, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating todo {Id} without transaction", id);
        
        return await _repository.UpdateAsync(id, title, isDone, ct);
    }

    /// <summary>
    /// Ví d?: Ð?c nhi?u todos trong cùng 1 transaction ð? ð?m b?o consistency
    /// (Ð?c snapshot t?i th?i ði?m b?t ð?u transaction)
    /// </summary>
    public async Task<(IReadOnlyList<TodoItemDto> All, int CompletedCount)> GetTodoStatisticsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Getting todo statistics WITH transaction for consistency");

        try
        {
            // B?T Ð?U transaction ð? ð?m b?o ð?c consistent data
            _unitOfWork.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

            var allTodos = await _repository.GetAllAsync(ct);
            var totalCount = await _repository.CountAsync(null, ct); // Pass null for search parameter

            _unitOfWork.Commit();

            return (allTodos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics, rolling back");
            _unitOfWork.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Ví d?: Custom transaction v?i isolation level khác
    /// </summary>
    public async Task<IReadOnlyList<TodoItemDto>> GetTodosWithSerializableIsolationAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Getting todos with Serializable isolation");

        try
        {
            // S? d?ng Serializable isolation ð? tránh phantom reads
            _unitOfWork.BeginTransaction(System.Data.IsolationLevel.Serializable);

            var todos = await _repository.GetAllAsync(ct);

            _unitOfWork.Commit();

            return todos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error with serializable read");
            _unitOfWork.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Ví d?: Read Uncommitted (dirty read) cho reports không c?n accuracy tuy?t ð?i
    /// </summary>
    public async Task<int> GetApproximateTodoCountAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Getting approximate count with ReadUncommitted");

        try
        {
            // ReadUncommitted: Nhanh nhýng có th? ð?c dirty data
            _unitOfWork.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);

            var count = await _repository.CountAsync(null, ct); // Pass null for search parameter

            _unitOfWork.Commit();

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approximate count");
            _unitOfWork.Rollback();
            throw;
        }
    }
}
