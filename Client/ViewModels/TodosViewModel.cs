using Shared.Entities.Dtos;
using Shared.Contracts;
using Client.Services;

namespace Client.ViewModels;

public class TodosViewModel
{
    private readonly ITodoApi _api;

    public IReadOnlyList<TodoItemDto> Items => _items;
    private List<TodoItemDto> _items = new();

    public bool IsLoading { get; private set; }
    public bool IsSaving { get; private set; }
    public string? Error { get; private set; }

    public TodosViewModel(ITodoApi api)
    {
        _api = api;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        Error = null;
        try
        {
            var resp = await _api.GetAllAsync(ct);
            if (resp.IsSuccess && resp.Data is not null)
                _items = resp.Data.ToList();
            else
                Error = resp.Message ?? "Unknown error";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> AddAsync(string? title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return false;
        IsSaving = true; Error = null;
        try
        {
            var resp = await _api.CreateAsync(new CreateTodoRequest(title), ct);
            if (!resp.IsSuccess || resp.Data is null)
            {
                Error = resp.Message ?? "Request failed";
                return false;
            }
            _items.Insert(0, resp.Data);
            return true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    public async Task<bool> ToggleAsync(TodoItemDto item, bool isDone, CancellationToken ct = default)
    {
        IsSaving = true; Error = null;
        try
        {
            var resp = await _api.UpdateAsync(item.Id, new UpdateTodoRequest(item.Title, isDone), ct);
            if (!resp.IsSuccess || resp.Data is null)
            {
                Error = resp.Message ?? "Request failed";
                return false;
            }
            var idx = _items.FindIndex(x => x.Id == item.Id);
            if (idx >= 0) _items[idx] = resp.Data;
            return true;
        }
        finally { IsSaving = false; }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        IsSaving = true; Error = null;
        try
        {
            var resp = await _api.DeleteAsync(id, ct);
            if (!resp.IsSuccess)
            {
                Error = resp.Message ?? "Request failed";
                return false;
            }
            _items.RemoveAll(x => x.Id == id);
            return true;
        }
        finally { IsSaving = false; }
    }
}
