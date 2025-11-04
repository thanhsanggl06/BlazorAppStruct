using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Shared.Entities.Dtos;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodosController(ITodoService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TodoItemDto>>> GetAll(CancellationToken ct)
            => Ok(await service.GetAllAsync(ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TodoItemDto>> GetById(int id, CancellationToken ct)
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<TodoItemDto>> Create([FromBody] CreateTodoRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title is required.");
            var created = await service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TodoItemDto>> Update(int id, [FromBody] UpdateTodoRequest request, CancellationToken ct)
        {
            var updated = await service.UpdateAsync(id, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await service.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
