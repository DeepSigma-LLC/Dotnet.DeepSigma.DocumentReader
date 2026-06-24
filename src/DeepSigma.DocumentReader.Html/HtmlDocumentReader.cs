using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DeepSigma.DocumentReader.Core.Readers;

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

        using var reader = new StreamReader(context.Stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        string html = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        using IDocument document = Parser.ParseDocument(html);

        var headings = new List<DocumentHeading>();
        var headingEntries = new List<HeadingEntry>();
        foreach (IElement element in document.QuerySelectorAll("h1,h2,h3,h4,h5,h6"))
        {
            int level = element.LocalName.Length == 2 && char.IsDigit(element.LocalName[1])
                ? element.LocalName[1] - '0'
                : 1;
            string headingText = element.TextContent.Trim();
            headings.Add(new DocumentHeading(level, headingText));
            headingEntries.Add(new HeadingEntry(level, headingText));
        }

        var tables = options.ExtractTables ? ConvertTables(document) : [];
        var links = options.ExtractLinks ? ConvertLinks(document) : [];

        return new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.Html),
            Kind = DocumentKind.Html,
            Text = context.Options.ExtractText ? TextExtractor.ExtractText(html) : null,
            Sections = SectionTreeBuilder.Build(headingEntries),
            Tables = tables,
            Metadata = new DocumentMetadata { Title = string.IsNullOrWhiteSpace(document.Title) ? null : document.Title },
            Quality = ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
            Features = [new HtmlDocumentFeature { Headings = headings, Links = links }],
        };
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
