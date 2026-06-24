using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DeepSigma.DocumentReader.Core.Readers;
using DeepSigma.DocumentReader.Core.Text;
using DeepSigma.DocumentReader.Html.Internal;

namespace DeepSigma.DocumentReader.Html;

/// <summary>
/// Reads HTML documents using AngleSharp: readable text, a heading-based section tree,
/// tables, links/images, and the document title. External resources are never loaded.
/// </summary>
public sealed class HtmlDocumentReader : FormatDocumentReaderBase
{
    private static readonly HtmlParser Parser = new();
    private static readonly HtmlTextExtractor TextExtractor = new();

    /// <inheritdoc />
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [DocumentKind.Html];

    /// <inheritdoc />
    protected override async Task<DocumentReadResult> ReadCoreAsync(DocumentReadContext context, CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<HtmlReadOptions>();

        string html = await TextContent.ReadAllTextAsync(context.Stream, cancellationToken).ConfigureAwait(false);

        using IDocument document = Parser.ParseDocument(html);

        (IReadOnlyList<DocumentHeading> headings, IReadOnlyList<HeadingEntry> headingEntries) = ExtractHeadings(document);

        var tables = options.ExtractTables ? ConvertTables(document) : [];
        var links = options.ExtractLinks ? ConvertLinks(document) : [];

        return new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.Html),
            Kind = DocumentKind.Html,
            Text = context.Options.ExtractText ? TextExtractor.ExtractText(document) : null,
            Sections = SectionTreeBuilder.Build(headingEntries),
            Tables = tables,
            Metadata = new DocumentMetadata { Title = string.IsNullOrWhiteSpace(document.Title) ? null : document.Title },
            Quality = ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
            Features = [new HtmlDocumentFeature { Headings = headings, Links = links }],
        };
    }

    /// <summary>
    /// Walks the document in order, capturing each heading and the text that follows it (up
    /// to the next heading) as the section body.
    /// </summary>
    private static (IReadOnlyList<DocumentHeading> Headings, IReadOnlyList<HeadingEntry> Entries) ExtractHeadings(IDocument document)
    {
        var headings = new List<DocumentHeading>();
        var sections = new List<SectionAccumulator>();
        SectionAccumulator? current = null;
        INode root = document.Body ?? (INode)document.DocumentElement;

        Walk(root);
        void Walk(INode node)
        {
            foreach (INode child in node.ChildNodes)
            {
                switch (child)
                {
                    case IText text:
                        current?.Body.Append(text.Data);
                        break;

                    case IElement element when !HtmlElements.Skip.Contains(element.LocalName):
                        if (IsHeading(element, out int level))
                        {
                            string title = element.TextContent.Trim();
                            headings.Add(new DocumentHeading(level, title));
                            current = new SectionAccumulator(level, title);
                            sections.Add(current);
                            break; // title captured; do not descend into the heading
                        }

                        Walk(element);
                        if (HtmlElements.Block.Contains(element.LocalName))
                        {
                            current?.Body.Append('\n');
                        }

                        break;
                }
            }
        }

        var entries = sections
            .Select(s => new HeadingEntry(s.Level, s.Title, HtmlElements.NormalizeOrNull(s.Body.ToString())))
            .ToList();
        return (headings, entries);
    }

    private static bool IsHeading(IElement element, out int level)
    {
        string name = element.LocalName;
        if (name.Length == 2 && name[0] is 'h' or 'H' && name[1] is >= '1' and <= '6')
        {
            level = name[1] - '0';
            return true;
        }

        level = 0;
        return false;
    }

    private static IReadOnlyList<DocumentTable> ConvertTables(IDocument document)
    {
        var tables = new List<DocumentTable>();
        foreach (IElement table in document.QuerySelectorAll("table"))
        {
            var builder = new DocumentTableBuilder();
            foreach (IElement row in table.QuerySelectorAll("tr"))
            {
                IHtmlCollection<IElement> cellElements = row.QuerySelectorAll("th,td");
                var cells = cellElements.Select(c => (string?)c.TextContent.Trim()).ToArray();
                bool isHeaderRow = cellElements.All(c => c.LocalName.Equals("th", StringComparison.OrdinalIgnoreCase));
                if (isHeaderRow && cellElements.Length > 0 && builder.Headers.Count == 0)
                {
                    builder.Headers = cells.Select(c => c ?? string.Empty).ToArray();
                }
                else
                {
                    builder.AddRow(cells);
                }
            }

            tables.Add(builder.Build());
        }

        return tables;
    }

    private static IReadOnlyList<DocumentLink> ConvertLinks(IDocument document)
    {
        var links = new List<DocumentLink>();
        foreach (IElement anchor in document.QuerySelectorAll("a[href]"))
        {
            links.Add(new DocumentLink(anchor.TextContent.Trim(), anchor.GetAttribute("href") ?? string.Empty, IsImage: false));
        }

        foreach (IElement image in document.QuerySelectorAll("img[src]"))
        {
            links.Add(new DocumentLink(image.GetAttribute("alt"), image.GetAttribute("src") ?? string.Empty, IsImage: true));
        }

        return links;
    }
}
