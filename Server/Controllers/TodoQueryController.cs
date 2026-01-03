using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Server.Controllers;

/// <summary>
/// Demo Controller: Cách s? d?ng Repository v?i/không transaction
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TodoQueryController : ControllerBase
{
    private readonly ITodoQueryService _service;
    private readonly ILogger<TodoQueryController> _logger;

    public TodoQueryController(
        ITodoQueryService service,
        ILogger<TodoQueryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get todo by ID - KHÔNG dùng transaction
    /// GET /api/todoquery/1
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetTodoById(int id, CancellationToken ct)
    {
        var todo = await _service.GetTodoByIdAsync(id, ct);
        
        if (todo == null)
            return NotFound(new { Message = $"Todo {id} not found" });

        return Ok(todo);
    }

    /// <summary>
    /// Get all todos - KHÔNG dùng transaction
    /// GET /api/todoquery
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAllTodos(CancellationToken ct)
    {
        var todos = await _service.GetAllTodosAsync(ct);
        return Ok(todos);
    }

    /// <summary>
    /// Create todo - KHÔNG dùng transaction (single operation)
    /// POST /api/todoquery
    /// Body: { "title": "Buy milk" }
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateTodo(
        [FromBody] CreateTodoQueryRequest request,
        CancellationToken ct)
    {
        var newId = await _service.CreateTodoAsync(request.Title, ct);
        
        return CreatedAtAction(
            nameof(GetTodoById),
            new { id = newId },
            new { Id = newId, Title = request.Title }
        );
    }

    /// <summary>
    /// Update todo - KHÔNG dùng transaction (single operation)
    /// PUT /api/todoquery/1
    /// Body: { "title": "Updated title", "isDone": true }
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTodo(
        int id,
        [FromBody] UpdateTodoQueryRequest request,
        CancellationToken ct)
    {
        var success = await _service.UpdateTodoAsync(id, request.Title, request.IsDone, ct);
        
        if (!success)
            return NotFound(new { Message = $"Todo {id} not found" });

        return NoContent();
    }

    /// <summary>
    /// Get statistics - DÙNG transaction ð? ð?m b?o consistency
    /// GET /api/todoquery/statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics(CancellationToken ct)
    {
        var (todos, completedCount) = await _service.GetTodoStatisticsAsync(ct);
        
        return Ok(new
        {
            TotalCount = todos.Count,
            CompletedCount = completedCount,
            PendingCount = todos.Count - completedCount,
            Todos = todos
        });
    }

    /// <summary>
    /// Get todos v?i Serializable isolation
    /// GET /api/todoquery/serializable
    /// </summary>
    [HttpGet("serializable")]
    public async Task<ActionResult> GetTodosSerializable(CancellationToken ct)
    {
        var todos = await _service.GetTodosWithSerializableIsolationAsync(ct);
        
        return Ok(new
        {
            IsolationLevel = "Serializable",
            Message = "Data read with highest isolation level - no phantom reads",
            Todos = todos
        });
    }

    /// <summary>
    /// Get approximate count v?i ReadUncommitted (dirty read)
    /// GET /api/todoquery/approximate-count
    /// </summary>
    [HttpGet("approximate-count")]
    public async Task<ActionResult> GetApproximateCount(CancellationToken ct)
    {
        var count = await _service.GetApproximateTodoCountAsync(ct);
        
        return Ok(new
        {
            IsolationLevel = "ReadUncommitted",
            Message = "Fast but may include uncommitted data",
            ApproximateCount = count
        });
    }
}

// Request DTOs
public record CreateTodoQueryRequest(string Title);
public record UpdateTodoQueryRequest(string Title, bool IsDone);
