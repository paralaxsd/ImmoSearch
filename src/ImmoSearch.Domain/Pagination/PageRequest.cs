namespace ImmoSearch.Domain.Pagination;

public record PageRequest(int Page = 1, int PageSize = 20)
{
    public int Page { get; init; } = Math.Max(1, Page);
    public int PageSize { get; init; } = Math.Clamp(PageSize, 1, 200);
    public int Skip => (Page - 1) * PageSize;
}
