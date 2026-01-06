namespace ImmoSearch.Domain.Repositories;

public sealed record ListingSort(string? SortBy = null, bool SortDesc = true);
