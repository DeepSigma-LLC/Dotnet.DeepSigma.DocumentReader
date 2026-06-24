using System.Text.RegularExpressions;

namespace DeepSigma.DocumentReader.Html.Internal;

/// <summary>
/// Shared HTML element classification and text normalization used by both the text extractor
/// and the document reader, keeping their DOM traversal rules in one place.
/// </summary>
internal static partial class HtmlElements
{
    /// <summary>Elements whose content is not readable text and is skipped entirely.</summary>
    public static readonly HashSet<string> Skip =
        new(StringComparer.OrdinalIgnoreCase) { "script", "style", "noscript", "head", "title", "template" };

    /// <summary>Block-level elements that introduce a line break after their content.</summary>
    public static readonly HashSet<string> Block = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "div", "section", "article", "header", "footer", "main", "aside",
        "ul", "ol", "li", "table", "tr", "blockquote", "pre", "figure", "br",
        "h1", "h2", "h3", "h4", "h5", "h6",
    };

    /// <summary>
    /// Collapses horizontal whitespace within each line, trims, drops empty lines, and joins
    /// with single newlines. Returns <see langword="null"/> when nothing remains.
    /// </summary>
    public static string? NormalizeOrNull(string text)
    {
        IEnumerable<string> lines = text
            .Split('\n')
            .Select(line => HorizontalWhitespace().Replace(line, " ").Trim())
            .Where(line => line.Length > 0);
        string normalized = string.Join("\n", lines);
        return normalized.Length == 0 ? null : normalized;
    }

    [GeneratedRegex("[ \\t\\f\\v\\r]+")]
    private static partial Regex HorizontalWhitespace();
}
