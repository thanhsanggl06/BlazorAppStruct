using Data.Dapper.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Shared.Entities.Dtos;

namespace Services.Implements;

/// <summary>
/// Service implementation v?i transaction ða tác v? s? d?ng Dapper UnitOfWork pattern
/// Mô ph?ng các use case th?c t? trong enterprise application
/// </summary>
public class TodoTransactionService : ITodoTransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoRepository _repository;
    private readonly ILogger<TodoTransactionService> _logger;

    public TodoTransactionService(
        IUnitOfWork unitOfWork,
        ITodoRepository repository,
        ILogger<TodoTransactionService> logger)
    {
        _unitOfWork = unitOfWork;
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<int>> CreateMultipleTodosAsync(
        IEnumerable<string> titles, CancellationToken ct = default)
    {
        var titleList = titles.ToList();
        _logger.LogInformation("Creating {Count} todos in a single transaction", titleList.Count);

        var newIds = new List<int>();

        try
        {
            _unitOfWork.BeginTransaction();

            foreach (var title in titleList)
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentException("Title không ðý?c r?ng");
                }

                var newId = await _repository.CreateAsync(title, ct);
                newIds.Add(newId);
                _logger.LogDebug("Created todo {Id} with title: {Title}", newId, title);
            }

            _unitOfWork.Commit();
            _logger.LogInformation("Successfully created {Count} todos", newIds.Count);

            return newIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multiple todos, rolling back transaction");
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<(int UpdatedCount, int NewTodoId)> UpdateMultipleAndCreateSummaryAsync(
        IEnumerable<int> idsToComplete, string summaryTitle, CancellationToken ct = default)
    {
        var idList = idsToComplete.ToList();
        _logger.LogInformation(
            "Completing {Count} todos and creating summary todo in transaction", 
            idList.Count);

        try
        {
            _unitOfWork.BeginTransaction();

            // Bý?c 1: Ðánh d?u hoàn thành t?t c? todos
            var updatedCount = 0;
            foreach (var id in idList)
            {
                var todo = await _repository.GetByIdAsync(id, ct);
                if (todo == null)
                {
                    throw new InvalidOperationException($"Todo {id} không t?n t?i");
                }

                var success = await _repository.UpdateAsync(id, todo.Title, isDone: true, ct);
                if (success)
                {
                    updatedCount++;
                    _logger.LogDebug("Marked todo {Id} as completed", id);
                }
            }

            // Bý?c 2: T?o todo t?ng k?t
            var summaryTodoId = await _repository.CreateAsync(
                $"{summaryTitle} - Completed {updatedCount} tasks", 
                ct);

            _logger.LogInformation(
                "Created summary todo {Id} for {Count} completed tasks", 
                summaryTodoId, 
                updatedCount);

            _unitOfWork.Commit();

            return (updatedCount, summaryTodoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateMultipleAndCreateSummary, rolling back");
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<int> ArchiveCompletedTodosAsync(
        IEnumerable<int> todoIds, CancellationToken ct = default)
    {
        var idList = todoIds.ToList();
        _logger.LogInformation("Archiving {Count} todos in transaction", idList.Count);

        try
        {
            _unitOfWork.BeginTransaction();

            var archivedCount = 0;

            foreach (var id in idList)
            {
                // Bý?c 1: L?y thông tin todo
                var todo = await _repository.GetByIdAsync(id, ct);
                if (todo == null)
                {
                    _logger.LogWarning("Todo {Id} not found, skipping", id);
                    continue;
                }

                // Bý?c 2: Ðánh d?u hoàn thành n?u chýa
                if (!todo.IsDone)
                {
                    await _repository.UpdateAsync(id, todo.Title, isDone: true, ct);
                    _logger.LogDebug("Marked todo {Id} as done", id);
                }

                // Bý?c 3: T?o b?n sao trong "archive" (? ðây gi? l?p b?ng cách t?o todo m?i v?i prefix)
                var archiveTitle = $"[ARCHIVED] {todo.Title}";
                await _repository.CreateAsync(archiveTitle, ct);

                // Bý?c 4: Xóa todo g?c
                var deleted = await _repository.DeleteAsync(id, ct);
                if (deleted)
                {
                    archivedCount++;
                    _logger.LogDebug("Archived and deleted todo {Id}", id);
                }
            }

            _unitOfWork.Commit();
            _logger.LogInformation("Successfully archived {Count} todos", archivedCount);

            return archivedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving todos, rolling back");
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<int> BulkDeleteWithValidationAsync(
        IEnumerable<int> todoIds, CancellationToken ct = default)
    {
        var idList = todoIds.ToList();
        _logger.LogInformation("Bulk deleting {Count} todos with validation", idList.Count);

        try
        {
            _unitOfWork.BeginTransaction();

            // Validation: ki?m tra t?t c? todos t?n t?i trý?c khi xóa
            var todosToDelete = new List<TodoItemDto>();
            foreach (var id in idList)
            {
                var todo = await _repository.GetByIdAsync(id, ct);
                if (todo == null)
                {
                    throw new InvalidOperationException(
                        $"Validation failed: Todo {id} không t?n t?i. Rolling back transaction.");
                }
                todosToDelete.Add(todo);
            }

            // Validation: không cho phép xóa todos chýa hoàn thành (business rule)
            var incompleteTodos = todosToDelete.Where(t => !t.IsDone).ToList();
            if (incompleteTodos.Any())
            {
                var incompleteIds = string.Join(", ", incompleteTodos.Select(t => t.Id));
                throw new InvalidOperationException(
                    $"Validation failed: Todos {incompleteIds} chýa hoàn thành. " +
                    "Không th? xóa. Rolling back transaction.");
            }

            // Th?c hi?n xóa
            var deletedCount = 0;
            foreach (var id in idList)
            {
                var deleted = await _repository.DeleteAsync(id, ct);
                if (deleted)
                {
                    deletedCount++;
                    _logger.LogDebug("Deleted todo {Id}", id);
                }
            }

            _unitOfWork.Commit();
            _logger.LogInformation("Successfully deleted {Count} todos", deletedCount);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk delete, rolling back");
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<int> CloneAndArchiveAsync(
        int todoId, string newTitle, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Cloning todo {Id} with new title and archiving original", 
            todoId);

        try
        {
            _unitOfWork.BeginTransaction();

            // Bý?c 1: L?y todo g?c
            var originalTodo = await _repository.GetByIdAsync(todoId, ct);
            if (originalTodo == null)
            {
                throw new InvalidOperationException($"Todo {todoId} không t?n t?i");
            }

            // Bý?c 2: T?o b?n clone v?i title m?i
            var clonedId = await _repository.CreateAsync(newTitle, ct);
            _logger.LogInformation(
                "Created clone todo {CloneId} from original {OriginalId}", 
                clonedId, 
                todoId);

            // Bý?c 3: Ðánh d?u todo g?c là archived (update title v?i prefix)
            var archivedTitle = $"[ARCHIVED] {originalTodo.Title}";
            await _repository.UpdateAsync(todoId, archivedTitle, isDone: true, ct);
            _logger.LogInformation("Archived original todo {Id}", todoId);

            // Bý?c 4: T?o log entry (gi? l?p - trong th?c t? có th? insert vào b?ng AuditLog)
            var logTitle = $"LOG: Cloned Todo#{todoId} -> Todo#{clonedId} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
            await _repository.CreateAsync(logTitle, ct);
            _logger.LogDebug("Created audit log entry");

            _unitOfWork.Commit();
            _logger.LogInformation(
                "Successfully cloned todo {OriginalId} to {ClonedId}", 
                todoId, 
                clonedId);

            return clonedId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CloneAndArchive, rolling back");
            _unitOfWork.Rollback();
            throw;
        }
    }
}
