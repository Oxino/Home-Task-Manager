namespace HomeTaskManager.Application.Abstractions;

/// <summary>Generic page of results returned by list use cases, with the total count needed to render pagination controls.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
