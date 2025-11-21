using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Shared.Contracts;
using Shared.Entities.Dtos;

namespace Server.Controllers;

[ApiController]
[Route("api/todo-sp")] // separate route for SP demo
public class TodoSpController(ITodoSpService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        var data = await service.GetAllAsync(search, ct);
        return Ok(ApiResponse.Success(data));
    }

    [HttpGet("paged")] // /api/todo-sp/paged?pageNumber=1&pageSize=20&search=abc
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var (items, total) = await service.GetPagedAsync(pageNumber, pageSize, search, ct);
        var payload = new { items, total, pageNumber, pageSize };
        return Ok(ApiResponse.Success(payload));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null
            ? NotFound(ApiResponse.Fail<object>("Todo not found", code: "NOT_FOUND"))
            : Ok(ApiResponse.Success(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTodoRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(ApiResponse.Fail<object>("Title is required", code: "VALIDATION"));
        var created = await service.CreateAsync(req.Title, ct);
        return CreatedAtAction(nameof(GetById), new { id = created!.Id }, ApiResponse.Success(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTodoRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(ApiResponse.Fail<object>("Title is required", code: "VALIDATION"));
        var updated = await service.UpdateAsync(id, req.Title, req.IsDone, ct);
        return updated is null
            ? NotFound(ApiResponse.Fail<object>("Todo not found", code: "NOT_FOUND"))
            : Ok(ApiResponse.Success(updated));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await service.DeleteAsync(id, ct);
        return ok
            ? Ok(ApiResponse.Success(new { Id = id }))
            : NotFound(ApiResponse.Fail<object>("Todo not found", code: "NOT_FOUND"));
    }
}
