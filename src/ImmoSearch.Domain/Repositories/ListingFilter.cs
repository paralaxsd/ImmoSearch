namespace ImmoSearch.Domain.Repositories;

public sealed record ListingFilter(
    string? City = null,
    string? Zip = null,
    string? Source = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    decimal? MinSize = null,
    decimal? MaxSize = null,
    decimal? MinRooms = null,
    decimal? MaxRooms = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    string? Query = null);
