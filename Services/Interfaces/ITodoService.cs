using Shared.Entities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ITodoService
    {
        Task<IReadOnlyList<TodoItemDto>> GetAllAsync(CancellationToken ct = default);
        Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<TodoItemDto> CreateAsync(CreateTodoRequest request, CancellationToken ct = default);
        Task<TodoItemDto?> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
