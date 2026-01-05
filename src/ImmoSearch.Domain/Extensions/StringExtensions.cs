using System.Diagnostics.CodeAnalysis;

namespace ImmoSearch.Domain.Extensions;

public static class StringExtensions
{
    extension([NotNullWhen(false)] string? text)
    {
        public bool NullOrEmpty => string.IsNullOrEmpty(text);
        public bool NullOrWhitespace => string.IsNullOrWhiteSpace(text);
    }

    extension([NotNullWhen(true)] string? text)
    {
        public bool HasContent => !text.NullOrEmpty;
        public bool HasText => !text.NullOrWhitespace;
    }
}
