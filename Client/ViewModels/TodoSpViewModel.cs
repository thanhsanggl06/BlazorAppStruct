using Client.Services;
using Shared.Entities.Dtos;

namespace Client.ViewModels;

public class TodoSpViewModel
{
    private readonly ITodoSpApi _api;

    public IReadOnlyList<TodoItemDto> Items => _items;
    private List<TodoItemDto> _items = new();

    public bool IsLoading { get; private set; }
    public bool IsSaving { get; private set; }
    public string? Error { get; private set; }

    public int PageNumber { get; set; } = 1; // setter public for UI adjustments
    public int PageSize { get; private set; } = 10;
    public int Total { get; private set; }
    public string? Search { get; set; }

    public TodoSpViewModel(ITodoSpApi api)
    {
        _api = api;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true; Error = null;
        try
        {
            var resp = await _api.GetPagedAsync(PageNumber, PageSize, Search, ct);
            if (!resp.IsSuccess || resp.Data is null)
            {
                Error = resp.Message ?? "Load failed";
                return;
            }
            // dynamic object (anonymous) -> use reflection
            var dataObj = resp.Data;
            var itemsProp = dataObj.GetType().GetProperty("items");
            var totalProp = dataObj.GetType().GetProperty("total");
            if (itemsProp?.GetValue(dataObj) is IEnumerable<TodoItemDto> items)
                _items = items.ToList();
            if (totalProp?.GetValue(dataObj) is int total)
                Total = total;
        }
        finally { IsLoading = false; }
    }

    public async Task<bool> AddAsync(string? title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return false;
        IsSaving = true; Error = null;
        try
        {
            var resp = await _api.CreateAsync(title, ct);
            if (!resp.IsSuccess || resp.Data is null)
            {
                Error = resp.Message ?? "Create failed";
                return false;
            }
            _items.Insert(0, resp.Data);
            Total++;
            return true;
        }
        finally { IsSaving = false; }
    }

    public async Task<bool> ToggleAsync(TodoItemDto item, bool isDone, CancellationToken ct = default)
    {
        IsSaving = true; Error = null;
        try
        {
            var resp = await _api.UpdateAsync(item.Id, item.Title, isDone, ct);
            if (!resp.IsSuccess || resp.Data is null)
            {
                Error = resp.Message ?? "Update failed";
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
                Error = resp.Message ?? "Delete failed";
                return false;
            }
            _items.RemoveAll(x => x.Id == id);
            Total = Math.Max(0, Total - 1);
            return true;
        }
        finally { IsSaving = false; }
    }

    public async Task NextPageAsync(CancellationToken ct = default)
    {
        if (PageNumber * PageSize >= Total) return;
        PageNumber++;
        await LoadAsync(ct);
    }

    public async Task PrevPageAsync(CancellationToken ct = default)
    {
        if (PageNumber <= 1) return;
        PageNumber--;
        await LoadAsync(ct);
    }
}
