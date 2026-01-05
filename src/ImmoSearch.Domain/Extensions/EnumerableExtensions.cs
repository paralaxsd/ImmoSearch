namespace ImmoSearch.Domain.Extensions;

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> values)
    {
        public string JoinedBy(string separator) =>
            string.Join(separator, values);
    }
}
