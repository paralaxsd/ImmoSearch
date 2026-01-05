using System.Diagnostics.CodeAnalysis;

namespace ImmoSearch.Domain.Helpers;

public static class ZipCodeParser
{
    public static IReadOnlyList<int> Parse(string raw)
    {
        var zips = TryParse(raw);
        if (zips is not null) return zips;
        throw new ArgumentException("Invalid ZIP codes", nameof(raw));
    }

    public static IReadOnlyList<int>? TryParse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var list = new List<int>();
        var seen = new HashSet<int>();

        foreach (var part in raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Length is < 3 or > 5) return null;
            if (!int.TryParse(part, out var zip)) return null;
            if (zip <= 0) return null;
            if (seen.Add(zip)) list.Add(zip);
        }

        return list.Count == 0 ? null : list;
    }
}
