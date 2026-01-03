using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Entities.Dtos
{
    // Converted to record class with explicit properties and parameterless constructor for reflection mapping support.
    public record TodoItemDto
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public bool IsDone { get; init; }
        public DateTime? CreatedAt { get; init; }

        public TodoItemDto() { }
        public TodoItemDto(int id, string title, bool isDone, DateTime createdAt)
        {
            Id = id;
            Title = title;
            IsDone = isDone;
            CreatedAt = createdAt;
        }
    }

    public record CreateTodoRequest(string Title)
    {
        public CreateTodoRequest() : this(string.Empty) { }
    }

    public record UpdateTodoRequest(string Title, bool IsDone)
    {
        public UpdateTodoRequest() : this(string.Empty, false) { }
    }
}
