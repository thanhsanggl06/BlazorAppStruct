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
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var data = await service.GetAllAsync(ct);
            return Ok(ApiResponse.Success<IReadOnlyList<TodoItemDto>>(data));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var item = await service.GetByIdAsync(id, ct);
            if (item is null)
                return NotFound(ApiResponse.Fail<object>("Todo not found", code: "NOT_FOUND"));
            return Ok(ApiResponse.Success(item));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTodoRequest request, CancellationToken ct)
        {
            var created = await service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse.Success(created));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTodoRequest request, CancellationToken ct)
        {
            var updated = await service.UpdateAsync(id, request, ct);
            if (updated is null)
                return NotFound(ApiResponse.Fail<object>("Todo not found", code: "NOT_FOUND"));
            return Ok(ApiResponse.Success(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await service.DeleteAsync(id, ct);
            if (!ok)
                return NotFound(ApiResponse.Fail<object>("Todo not found", code: "NOT_FOUND"));
            return Ok(ApiResponse.Success(new { Id = id }));
        }
    }
}
