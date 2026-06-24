using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace DeepSigma.DocumentReader.Html;

/// <summary>
/// Extracts readable plain text from HTML using AngleSharp, inserting line breaks at block
/// boundaries and dropping script/style content. Implements <see cref="IHtmlTextExtractor"/>
/// so other readers (e.g. email) can reuse it.
/// </summary>
public sealed partial class HtmlTextExtractor : IHtmlTextExtractor
{
    private static readonly HtmlParser Parser = new();

    private static readonly HashSet<string> SkipElements =
        new(StringComparer.OrdinalIgnoreCase) { "script", "style", "noscript", "head", "title", "template" };

    private static readonly HashSet<string> BlockElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "div", "section", "article", "header", "footer", "main", "aside",
        "ul", "ol", "li", "table", "tr", "blockquote", "pre", "figure",
        "h1", "h2", "h3", "h4", "h5", "h6",
    };

    /// <inheritdoc />
    public string ExtractText(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        using IDocument document = Parser.ParseDocument(html);
        INode root = document.Body ?? (INode)document.DocumentElement;

        var builder = new StringBuilder();
        Walk(root, builder);
        return Normalize(builder.ToString());
    }

    private static void Walk(INode node, StringBuilder builder)
    {
        foreach (INode child in node.ChildNodes)
        {
            switch (child)
            {
                case IText text:
                    builder.Append(text.Data);
                    break;

                case IElement element:
                    if (SkipElements.Contains(element.LocalName))
                    {
                        continue;
                    }

                    if (element.LocalName.Equals("br", StringComparison.OrdinalIgnoreCase))
                    {
                        builder.Append('\n');
                        continue;
                    }

                    Walk(element, builder);
                    if (BlockElements.Contains(element.LocalName))
                    {
                        builder.Append('\n');
                    }

                    break;
            }
        }
    }

    private static string Normalize(string text)
    {
        IEnumerable<string> lines = text
            .Split('\n')
            .Select(line => HorizontalWhitespace().Replace(line, " ").Trim())
            .Where(line => line.Length > 0);
        return string.Join("\n", lines);
    }

    [GeneratedRegex("[ \\t\\f\\v\\r]+")]
    private static partial Regex HorizontalWhitespace();
}
