using System.Text;
using DeepSigma.DocumentReader.Core.Readers;
using DeepSigma.DocumentReader.Plaintext.Internal;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace DeepSigma.DocumentReader.Plaintext;

/// <summary>
/// Reads Markdown documents using Markdig. Builds a section tree from headings, converts
/// pipe tables to <see cref="DocumentTable"/>, and collects code blocks, links, and YAML
/// front matter into a <see cref="MarkdownDocumentFeature"/>.
/// </summary>
public sealed class MarkdownDocumentReader : FormatDocumentReaderBase
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseYamlFrontMatter()
        .UseAutoIdentifiers()
        .Build();

    /// <inheritdoc />
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [DocumentKind.Markdown];

    /// <inheritdoc />
    protected override async Task<DocumentReadResult> ReadCoreAsync(
        DocumentReadContext context,
        CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<MarkdownReadOptions>();
        byte[] bytes = await ContentReader.ReadAllBytesAsync(context.Stream, cancellationToken).ConfigureAwait(false);
        string text = ContentReader.DecodeUtf8(bytes);

        MarkdownDocument document = Markdown.Parse(text, Pipeline);

        var headings = new List<DocumentHeading>();
        var headingEntries = new List<HeadingEntry>();
        foreach (HeadingBlock heading in document.Descendants<HeadingBlock>())
        {
            string headingText = GetInlineText(heading.Inline);
            headings.Add(new DocumentHeading(heading.Level, headingText));
            headingEntries.Add(new HeadingEntry(heading.Level, headingText));
        }

        var tables = context.Options.ExtractTables ? ConvertTables(document) : [];
        var codeBlocks = options.ExtractCodeBlocks ? ConvertCodeBlocks(document) : [];
        var links = options.ExtractLinks ? ConvertLinks(document) : [];
        var frontMatter = options.ExtractFrontMatter ? ParseFrontMatter(document) : new Dictionary<string, string>();

        string? projection = context.Options.ExtractText
            ? options.PreserveMarkdown ? text : Markdown.ToPlainText(text, Pipeline)
            : null;

        var feature = new MarkdownDocumentFeature
        {
            Headings = headings,
            CodeBlocks = codeBlocks,
            Links = links,
            FrontMatter = frontMatter,
        };

        return new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.Markdown),
            Kind = DocumentKind.Markdown,
            Text = projection,
            Sections = SectionTreeBuilder.Build(headingEntries),
            Tables = tables,
            Quality = ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
            Features = [feature],
        };
    }

    private static IReadOnlyList<DocumentTable> ConvertTables(MarkdownDocument document)
    {
        var tables = new List<DocumentTable>();
        foreach (Table table in document.Descendants<Table>())
        {
            var builder = new DocumentTableBuilder();
            foreach (var rowObject in table)
            {
                if (rowObject is not TableRow row)
                {
                    continue;
                }

                var cells = new List<string?>();
                foreach (var cellObject in row)
                {
                    if (cellObject is TableCell cell)
                    {
                        cells.Add(GetBlockText(cell));
                    }
                }

                if (row.IsHeader && builder.Headers.Count == 0)
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

    private static IReadOnlyList<DocumentCodeBlock> ConvertCodeBlocks(MarkdownDocument document)
    {
        var blocks = new List<DocumentCodeBlock>();
        foreach (CodeBlock code in document.Descendants<CodeBlock>())
        {
            // YAML front matter derives from CodeBlock; it is surfaced separately.
            if (code is YamlFrontMatterBlock)
            {
                continue;
            }

            string? language = (code as FencedCodeBlock)?.Info;
            blocks.Add(new DocumentCodeBlock(
                string.IsNullOrEmpty(language) ? null : language,
                GetLinesText(code)));
        }

        return blocks;
    }

    private static IReadOnlyList<DocumentLink> ConvertLinks(MarkdownDocument document)
    {
        var links = new List<DocumentLink>();
        foreach (LinkInline link in document.Descendants<LinkInline>())
        {
            links.Add(new DocumentLink(GetInlineText(link), link.Url ?? string.Empty, link.IsImage));
        }

        return links;
    }

    private static Dictionary<string, string> ParseFrontMatter(MarkdownDocument document)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var block = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        if (block is null)
        {
            return result;
        }

        foreach (string rawLine in GetLinesText(block).Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line == "---")
            {
                continue;
            }

            int colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0)
            {
                continue;
            }

            string key = line[..colon].Trim();
            string value = line[(colon + 1)..].Trim().Trim('"', '\'');
            if (key.Length > 0)
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static string GetInlineText(ContainerInline? container)
    {
        if (container is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (LiteralInline literal in container.Descendants<LiteralInline>())
        {
            builder.Append(literal.Content.ToString());
        }

        return builder.ToString();
    }

    private static string GetBlockText(MarkdownObject markdownObject)
    {
        var builder = new StringBuilder();
        foreach (LiteralInline literal in markdownObject.Descendants<LiteralInline>())
        {
            builder.Append(literal.Content.ToString());
        }

        return builder.ToString();
    }

    private static string GetLinesText(LeafBlock block)
    {
        var builder = new StringBuilder();
        var lines = block.Lines.Lines;
        for (int i = 0; i < block.Lines.Count; i++)
        {
            builder.AppendLine(lines[i].Slice.ToString());
        }

        return builder.ToString();
    }
}
