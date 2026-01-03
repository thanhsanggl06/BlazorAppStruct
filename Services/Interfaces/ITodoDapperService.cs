namespace Services.Interfaces;

using Shared.Entities.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ITodoDapperService
{
    Task<IEnumerable<TodoItemDto>> GetAllAsync();
    Task<TodoItemDto?> GetByIdAsync(int id);
}

public record TodoDto(int Id, string Title, bool IsDone);