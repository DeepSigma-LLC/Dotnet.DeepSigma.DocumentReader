using System.Text;
using DeepSigma.DocumentReader.Core.Readers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;

namespace DeepSigma.DocumentReader.Office;

/// <summary>
/// Reads PowerPoint (PPTX) presentations using the Open XML SDK: per-slide title, body text,
/// speaker notes, and tables, plus a slide-per-section tree. Macros are never executed.
/// </summary>
public sealed class PowerPointDocumentReader : FormatDocumentReaderBase
{
    /// <inheritdoc />
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [DocumentKind.Presentation];

    /// <inheritdoc />
    protected override Task<DocumentReadResult> ReadCoreAsync(DocumentReadContext context, CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<PowerPointReadOptions>();

        using var document = PresentationDocument.Open(context.Stream, isEditable: false);
        PresentationPart? presentationPart = document.PresentationPart;

        var slides = new List<PresentationSlide>();
        var tables = new List<DocumentTable>();
        var headings = new List<HeadingEntry>();
        var text = new StringBuilder();

        SlideIdList? slideIdList = presentationPart?.Presentation?.SlideIdList;
        if (presentationPart is not null && slideIdList is not null)
        {
            int slideNumber = 0;
            foreach (SlideId slideId in slideIdList.Elements<SlideId>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                slideNumber++;

                if (slideId.RelationshipId?.Value is not { } relationshipId
                    || presentationPart.GetPartById(relationshipId) is not SlidePart slidePart
                    || slidePart.Slide is not { } slide)
                {
                    continue;
                }

                bool hidden = slide.Show is { } show && !show.Value;
                if (hidden && !options.IncludeHiddenSlides)
                {
                    context.AddWarning(WarningCodes.PowerPointHiddenSlideSkipped,
                        $"Hidden slide {slideNumber} was skipped.",
                        new DocumentLocation(SlideNumber: slideNumber));
                    continue;
                }

                PresentationSlide built = ReadSlide(slide, slidePart, slideNumber, options);
                slides.Add(built);
                tables.AddRange(built.Tables);
                headings.Add(new HeadingEntry(1, $"Slide {slideNumber}: {built.Title}".TrimEnd(':', ' ')));
                AppendSlideText(text, built, options);
            }
        }

        var result = new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.Presentation),
            Kind = DocumentKind.Presentation,
            Text = context.Options.ExtractText ? text.ToString().TrimEnd('\n') : null,
            Sections = SectionTreeBuilder.Build(headings),
            Tables = context.Options.ExtractTables ? tables : [],
            Metadata = ReadMetadata(document),
            Quality = ExtractionQuality.High,
            Warnings = context.Warnings.ToArray(),
            Features = [new PresentationDocumentFeature { Slides = slides }],
        };

        return Task.FromResult(result);
    }

    private static PresentationSlide ReadSlide(Slide slide, SlidePart slidePart, int slideNumber, PowerPointReadOptions options)
    {
        string? title = null;
        var bodyText = new StringBuilder();

        foreach (Shape shape in slide.Descendants<Shape>())
        {
            string shapeText = GetShapeText(shape);
            if (shapeText.Length == 0)
            {
                continue;
            }

            if (title is null && IsTitle(shape))
            {
                title = shapeText;
            }
            else if (options.ExtractSlideText)
            {
                bodyText.Append(shapeText).Append('\n');
            }
        }

        var slideTables = options.ExtractTables ? ReadTables(slide, slideNumber) : [];

        string? notes = options.ExtractSpeakerNotes ? GetNotes(slidePart) : null;

        return new PresentationSlide
        {
            SlideNumber = slideNumber,
            Title = title,
            Text = bodyText.Length > 0 ? bodyText.ToString().TrimEnd('\n') : null,
            SpeakerNotes = string.IsNullOrWhiteSpace(notes) ? null : notes,
            Tables = slideTables,
        };
    }

    private static IReadOnlyList<DocumentTable> ReadTables(Slide slide, int slideNumber)
    {
        var tables = new List<DocumentTable>();
        foreach (D.Table table in slide.Descendants<D.Table>())
        {
            var builder = new DocumentTableBuilder
            {
                Location = new DocumentLocation(SlideNumber: slideNumber),
            };

            foreach (D.TableRow row in table.Elements<D.TableRow>())
            {
                var cells = row.Elements<D.TableCell>()
                    .Select(cell => (string?)GetDrawingText(cell))
                    .ToArray();
                builder.AddRow(cells);
            }

            tables.Add(builder.Build());
        }

        return tables;
    }

    private static string GetShapeText(Shape shape)
    {
        if (shape.TextBody is not { } body)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (D.Paragraph paragraph in body.Elements<D.Paragraph>())
        {
            foreach (D.Text run in paragraph.Descendants<D.Text>())
            {
                builder.Append(run.Text);
            }

            builder.Append('\n');
        }

        return builder.ToString().TrimEnd('\n');
    }

    private static string GetDrawingText(D.TableCell cell)
        => string.Concat(cell.Descendants<D.Text>().Select(t => t.Text));

    private static string? GetNotes(SlidePart slidePart)
    {
        NotesSlidePart? notesPart = slidePart.NotesSlidePart;
        if (notesPart?.NotesSlide is not { } notesSlide)
        {
            return null;
        }

        var builder = new StringBuilder();
        foreach (D.Paragraph paragraph in notesSlide.Descendants<D.Paragraph>())
        {
            string line = string.Concat(paragraph.Descendants<D.Text>().Select(t => t.Text));
            if (line.Length > 0)
            {
                builder.Append(line).Append('\n');
            }
        }

        return builder.ToString().TrimEnd('\n');
    }

    private static bool IsTitle(Shape shape)
    {
        var placeholder = shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.PlaceholderShape;
        if (placeholder?.Type is not { } type)
        {
            return false;
        }

        return type == PlaceholderValues.Title || type == PlaceholderValues.CenteredTitle;
    }

    private static void AppendSlideText(StringBuilder text, PresentationSlide slide, PowerPointReadOptions options)
    {
        text.Append("# Slide ").Append(slide.SlideNumber);
        if (!string.IsNullOrEmpty(slide.Title))
        {
            text.Append(": ").Append(slide.Title);
        }

        text.Append('\n');

        if (options.ExtractSlideText && !string.IsNullOrEmpty(slide.Text))
        {
            text.Append(slide.Text).Append('\n');
        }

        if (options.ExtractSpeakerNotes && !string.IsNullOrEmpty(slide.SpeakerNotes))
        {
            text.Append("[Notes] ").Append(slide.SpeakerNotes).Append('\n');
        }

        text.Append('\n');
    }

    private static DocumentMetadata ReadMetadata(PresentationDocument document)
    {
        var properties = document.PackageProperties;
        return new DocumentMetadata
        {
            Title = properties.Title,
            Author = properties.Creator,
            CreatedUtc = OfficeMetadata.ToOffset(properties.Created),
            ModifiedUtc = OfficeMetadata.ToOffset(properties.Modified),
        };
    }
}
