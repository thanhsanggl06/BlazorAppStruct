using Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Shared.Entities.Dtos;
using Shared.Entities.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements
{
    public class TodoService(AppDbContext db) : ITodoService
    {
        public async Task<IReadOnlyList<TodoItemDto>> GetAllAsync(CancellationToken ct = default)
        {
            return await db.TodoItems
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new TodoItemDto(x.Id, x.Title, x.IsDone, x.CreatedAt))
                .ToListAsync(ct);
        }

        public async Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await db.TodoItems
                .Where(x => x.Id == id)
                .Select(x => new TodoItemDto(x.Id, x.Title, x.IsDone, x.CreatedAt))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<TodoItemDto> CreateAsync(CreateTodoRequest request, CancellationToken ct = default)
        {
            var entity = new TodoItem
            {
                Title = request.Title.Trim(),
                IsDone = false,
                CreatedAt = DateTime.UtcNow
            };

            db.TodoItems.Add(entity);
            await db.SaveChangesAsync(ct);

            return new TodoItemDto(entity.Id, entity.Title, entity.IsDone, entity.CreatedAt);
        }

        public async Task<TodoItemDto?> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken ct = default)
        {
            var entity = await db.TodoItems.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return null;

            entity.Title = request.Title.Trim();
            entity.IsDone = request.IsDone;

            await db.SaveChangesAsync(ct);

            return new TodoItemDto(entity.Id, entity.Title, entity.IsDone, entity.CreatedAt);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await db.TodoItems.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return false;

            db.TodoItems.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }
    }
}
