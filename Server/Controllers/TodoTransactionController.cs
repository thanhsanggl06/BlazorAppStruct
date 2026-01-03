using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Server.Controllers;

/// <summary>
/// API Controller demo Dapper Transaction v?i nhi?u tác v?
/// S? d?ng UnitOfWork pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TodoTransactionController : ControllerBase
{
    private readonly ITodoTransactionService _service;
    private readonly ILogger<TodoTransactionController> _logger;

    public TodoTransactionController(
        ITodoTransactionService service,
        ILogger<TodoTransactionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// T?o nhi?u todos trong m?t transaction
    /// POST /api/todotransaction/create-multiple
    /// Body: ["Todo 1", "Todo 2", "Todo 3"]
    /// </summary>
    [HttpPost("create-multiple")]
    public async Task<ActionResult<IReadOnlyList<int>>> CreateMultipleTodos(
        [FromBody] List<string> titles,
        CancellationToken ct)
    {
        try
        {
            var newIds = await _service.CreateMultipleTodosAsync(titles, ct);
            return Ok(new
            {
                Success = true,
                Message = $"Created {newIds.Count} todos successfully",
                TodoIds = newIds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multiple todos");
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Hoàn thành nhi?u todos và t?o todo t?ng k?t
    /// POST /api/todotransaction/complete-and-summarize
    /// Body: { "todoIds": [1, 2, 3], "summaryTitle": "Weekly Summary" }
    /// </summary>
    [HttpPost("complete-and-summarize")]
    public async Task<ActionResult> CompleteAndSummarize(
        [FromBody] CompleteAndSummarizeRequest request,
        CancellationToken ct)
    {
        try
        {
            var (updatedCount, summaryId) = await _service.UpdateMultipleAndCreateSummaryAsync(
                request.TodoIds,
                request.SummaryTitle,
                ct);

            return Ok(new
            {
                Success = true,
                Message = $"Completed {updatedCount} todos and created summary",
                CompletedCount = updatedCount,
                SummaryTodoId = summaryId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in complete and summarize");
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Archive (lýu tr?) các todos ð? hoàn thành
    /// POST /api/todotransaction/archive
    /// Body: { "todoIds": [1, 2, 3] }
    /// </summary>
    [HttpPost("archive")]
    public async Task<ActionResult> ArchiveCompletedTodos(
        [FromBody] ArchiveRequest request,
        CancellationToken ct)
    {
        try
        {
            var archivedCount = await _service.ArchiveCompletedTodosAsync(request.TodoIds, ct);

            return Ok(new
            {
                Success = true,
                Message = $"Archived {archivedCount} todos",
                ArchivedCount = archivedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving todos");
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Xóa hàng lo?t v?i validation - ch? xóa n?u t?t c? todos h?p l?
    /// DELETE /api/todotransaction/bulk-delete
    /// Body: { "todoIds": [1, 2, 3] }
    /// </summary>
    [HttpDelete("bulk-delete")]
    public async Task<ActionResult> BulkDeleteWithValidation(
        [FromBody] BulkDeleteRequest request,
        CancellationToken ct)
    {
        try
        {
            var deletedCount = await _service.BulkDeleteWithValidationAsync(request.TodoIds, ct);

            return Ok(new
            {
                Success = true,
                Message = $"Deleted {deletedCount} todos",
                DeletedCount = deletedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk delete");
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Clone m?t todo và archive b?n g?c
    /// POST /api/todotransaction/clone-and-archive/{id}
    /// Body: { "newTitle": "New version of todo" }
    /// </summary>
    [HttpPost("clone-and-archive/{id}")]
    public async Task<ActionResult> CloneAndArchive(
        int id,
        [FromBody] CloneRequest request,
        CancellationToken ct)
    {
        try
        {
            var clonedId = await _service.CloneAndArchiveAsync(id, request.NewTitle, ct);

            return Ok(new
            {
                Success = true,
                Message = "Todo cloned and original archived",
                OriginalTodoId = id,
                ClonedTodoId = clonedId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in clone and archive");
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}

// Request DTOs
public record CompleteAndSummarizeRequest(List<int> TodoIds, string SummaryTitle);
public record ArchiveRequest(List<int> TodoIds);
public record BulkDeleteRequest(List<int> TodoIds);
public record CloneRequest(string NewTitle);
