using System.Collections.Generic;

namespace Shared.Entities.Dtos;

public record PagedTodosDto(IReadOnlyList<TodoItemDto> Items, int Total, int PageNumber, int PageSize);
