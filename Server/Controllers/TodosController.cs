using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Shared.Entities.Dtos;
using Shared.Contracts;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodosController(ITodoService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TodoItemDto>>>> GetAll(CancellationToken ct)
        {
            var data = await service.GetAllAsync(ct);
            return Ok(ApiResponse.Ok(data));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<TodoItemDto>>> GetById(int id, CancellationToken ct)
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null
                ? NotFound(ApiResponse.Fail<TodoItemDto>("NOT_FOUND", new[] { "Todo not found" }))
                : Ok(ApiResponse.Ok(item));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<TodoItemDto>>> Create([FromBody] CreateTodoRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(ApiResponse.Fail<TodoItemDto>("VALIDATION", new[] { "Title is required" }));
            if (request.Title.Length > 200)
                return BadRequest(ApiResponse.Fail<TodoItemDto>("VALIDATION", new[] { "Title max length 200" }));

            var created = await service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse.Ok(created));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<TodoItemDto>>> Update(int id, [FromBody] UpdateTodoRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(ApiResponse.Fail<TodoItemDto>("VALIDATION", new[] { "Title is required" }));
            if (request.Title.Length > 200)
                return BadRequest(ApiResponse.Fail<TodoItemDto>("VALIDATION", new[] { "Title max length 200" }));

            var updated = await service.UpdateAsync(id, request, ct);
            return updated is null
                ? NotFound(ApiResponse.Fail<TodoItemDto>("NOT_FOUND", new[] { "Todo not found" }))
                : Ok(ApiResponse.Ok(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct)
        {
            var ok = await service.DeleteAsync(id, ct);
            return ok
                ? Ok(ApiResponse.Ok<object>(new { Id = id }))
                : NotFound(ApiResponse.Fail<object>("NOT_FOUND", new[] { "Todo not found" }));
        }
    }
}
