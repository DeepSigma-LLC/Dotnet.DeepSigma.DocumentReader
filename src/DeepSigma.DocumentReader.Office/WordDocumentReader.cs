using System.Text;
using DeepSigma.DocumentReader.Core.Readers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DeepSigma.DocumentReader.Office;

/// <summary>
/// Reads Word (DOCX) documents using the Open XML SDK: paragraph prose, a heading-based
/// section tree, tables, and core metadata. Macros are never executed. Headers/footers,
/// footnotes/endnotes, and comments are appended only when their options are enabled.
/// </summary>
public sealed class WordDocumentReader : FormatDocumentReaderBase
{
    /// <inheritdoc />
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [DocumentKind.WordDocument];

    /// <inheritdoc />
    protected override Task<DocumentReadResult> ReadCoreAsync(DocumentReadContext context, CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<WordReadOptions>();

        using var document = WordprocessingDocument.Open(context.Stream, isEditable: false);
        MainDocumentPart? mainPart = document.MainDocumentPart;
        Body? body = mainPart?.Document?.Body;

        var text = new StringBuilder();
        var sections = new List<SectionAccumulator>();
        SectionAccumulator? currentSection = null;
        var tables = new List<DocumentTable>();

        if (body is not null)
        {
            foreach (OpenXmlElement element in body.ChildElements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (element)
                {
                    case Paragraph paragraph:
                        string content = GetParagraphText(paragraph, options.IncludeDeletedText);
                        if (content.Length == 0)
                        {
                            break;
                        }

                        text.Append(content).Append('\n');
                        if (HeadingLevel(paragraph) is { } level)
                        {
                            currentSection = new SectionAccumulator(level, content);
                            sections.Add(currentSection);
                        }
                        else
                        {
                            currentSection?.Body.Append(content).Append('\n');
                        }

                        break;

                    case Table table when options.ExtractTables:
                        tables.Add(ConvertTable(table));
                        break;
                }
            }
        }

        var headings = sections
            .Select(s => new HeadingEntry(s.Level, s.Title, NullIfEmpty(s.Body)))
            .ToList();

        if (options.IncludeHeadersAndFooters && mainPart is not null)
        {
            AppendHeadersAndFooters(mainPart, text);
        }

        if (options.IncludeFootnotes && mainPart is not null)
        {
            AppendNotes(mainPart, text);
        }

        if (options.IncludeComments && mainPart is not null)
        {
            AppendComments(mainPart, text);
        }

        var result = new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.WordDocument),
            Kind = DocumentKind.WordDocument,
            Text = context.Options.ExtractText ? text.ToString().TrimEnd('\n') : null,
            Sections = SectionTreeBuilder.Build(headings),
            Tables = tables,
            Metadata = ReadMetadata(document),
            Quality = ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
        };

        return Task.FromResult(result);
    }

    private sealed class SectionAccumulator(int level, string title)
    {
        public int Level { get; } = level;
        public string Title { get; } = title;
        public StringBuilder Body { get; } = new();
    }

    private static string? NullIfEmpty(StringBuilder body)
        => body.Length == 0 ? null : body.ToString().TrimEnd('\n');

    private static string GetParagraphText(Paragraph paragraph, bool includeDeleted)
    {
        var builder = new StringBuilder();
        foreach (OpenXmlElement element in paragraph.Descendants())
        {
            switch (element)
            {
                case Text t:
                    builder.Append(t.Text);
                    break;
                case DeletedText dt when includeDeleted:
                    builder.Append(dt.Text);
                    break;
                case TabChar:
                    builder.Append('\t');
                    break;
                case Break:
                case CarriageReturn:
                    builder.Append('\n');
                    break;
            }
        }

        return builder.ToString();
    }

    private static int? HeadingLevel(Paragraph paragraph)
    {
        string? styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (styleId is null || !styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string digits = new([.. styleId.Where(char.IsDigit)]);
        return int.TryParse(digits, out int level) && level is >= 1 and <= 9 ? level : null;
    }

    private static DocumentTable ConvertTable(Table table)
    {
        var builder = new DocumentTableBuilder();
        foreach (TableRow row in table.Elements<TableRow>())
        {
            var cells = row.Elements<TableCell>()
                .Select(cell => (string?)cell.InnerText)
                .ToArray();
            builder.AddRow(cells);
        }

        return builder.Build();
    }

    private static void AppendHeadersAndFooters(MainDocumentPart mainPart, StringBuilder text)
    {
        foreach (HeaderPart header in mainPart.HeaderParts)
        {
            AppendIfNotEmpty(text, header.Header?.InnerText);
        }

        foreach (FooterPart footer in mainPart.FooterParts)
        {
            AppendIfNotEmpty(text, footer.Footer?.InnerText);
        }
    }

    private static void AppendNotes(MainDocumentPart mainPart, StringBuilder text)
    {
        if (mainPart.FootnotesPart?.Footnotes is { } footnotes)
        {
            foreach (Footnote footnote in footnotes.Elements<Footnote>())
            {
                AppendIfNotEmpty(text, footnote.InnerText);
            }
        }

        if (mainPart.EndnotesPart?.Endnotes is { } endnotes)
        {
            foreach (Endnote endnote in endnotes.Elements<Endnote>())
            {
                AppendIfNotEmpty(text, endnote.InnerText);
            }
        }
    }

    private static void AppendComments(MainDocumentPart mainPart, StringBuilder text)
    {
        if (mainPart.WordprocessingCommentsPart?.Comments is { } comments)
        {
            foreach (Comment comment in comments.Elements<Comment>())
            {
                AppendIfNotEmpty(text, comment.InnerText);
            }
        }
    }

    private static void AppendIfNotEmpty(StringBuilder text, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            text.Append(value).Append('\n');
        }
    }

    private static DocumentMetadata ReadMetadata(WordprocessingDocument document)
    {
        var properties = document.PackageProperties;
        return new DocumentMetadata
        {
            Title = properties.Title,
            Author = properties.Creator,
            CreatedUtc = OfficeMetadata.ToOffset(properties.Created),
            ModifiedUtc = OfficeMetadata.ToOffset(properties.Modified),
            Language = properties.Language,
        };
    }
}
