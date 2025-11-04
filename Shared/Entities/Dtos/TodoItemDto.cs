using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Entities.Dtos
{
    public record TodoItemDto(
        int Id,
        string Title,
        bool IsDone,
        DateTime CreatedAt
    );

    public record CreateTodoRequest(string Title);

    public record UpdateTodoRequest(string Title, bool IsDone);
}
