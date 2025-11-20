namespace Shared.Contracts;

public record PagingRequest(int PageNumber = 1, int PageSize = 20)
{
    public int PageNumber { get; init; } = PageNumber <= 0 ? 1 : PageNumber;
    public int PageSize { get; init; } = PageSize is <= 0 or > 200 ? 20 : PageSize;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
