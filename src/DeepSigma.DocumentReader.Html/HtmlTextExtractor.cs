using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DeepSigma.DocumentReader.Html.Internal;

namespace DeepSigma.DocumentReader.Html;

/// <summary>
/// Extracts readable plain text from HTML using AngleSharp, inserting line breaks at block
/// boundaries and dropping script/style content. Implements <see cref="IHtmlTextExtractor"/>
/// so other readers (e.g. email) can reuse it.
/// </summary>
public sealed class HtmlTextExtractor : IHtmlTextExtractor
{
    private static readonly HtmlParser Parser = new();

    /// <inheritdoc />
    public string ExtractText(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        using IDocument document = Parser.ParseDocument(html);
        return ExtractText(document);
    }

    /// <summary>Extracts readable plain text from an already-parsed document.</summary>
    public string ExtractText(IDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        INode root = document.Body ?? (INode)document.DocumentElement;
        var builder = new StringBuilder();
        Walk(root, builder);
        return HtmlElements.NormalizeOrNull(builder.ToString()) ?? string.Empty;
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

                case IElement element when !HtmlElements.Skip.Contains(element.LocalName):
                    Walk(element, builder);
                    if (HtmlElements.Block.Contains(element.LocalName))
                    {
                        builder.Append('\n');
                    }

                    break;
            }
        }
    }
}
