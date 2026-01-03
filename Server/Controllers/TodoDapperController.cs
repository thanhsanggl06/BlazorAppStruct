using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Threading.Tasks;

namespace Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoDapperController : ControllerBase
{
    private readonly ITodoDapperService _service;

    public TodoDapperController(ITodoDapperService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is not null ? Ok(item) : NotFound();
    }
}
