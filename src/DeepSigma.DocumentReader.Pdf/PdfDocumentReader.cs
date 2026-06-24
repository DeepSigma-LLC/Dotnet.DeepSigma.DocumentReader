using System.Text;
using DeepSigma.DocumentReader.Core.Readers;
using DeepSigma.DocumentReader.Pdf.Internal;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DeepSigma.DocumentReader.Pdf;

/// <summary>
/// Reads PDF documents using PdfPig: page-based text extraction, a page model, and basic
/// metadata. Text extraction is best-effort; pages with no text layer (typically scanned)
/// are flagged with a warning rather than silently returning empty text. OCR is not applied.
/// </summary>
public sealed class PdfDocumentReader : FormatDocumentReaderBase
{
    /// <inheritdoc />
    public override IReadOnlyCollection<DocumentKind> SupportedKinds { get; } = [DocumentKind.Pdf];

    /// <inheritdoc />
    protected override Task<DocumentReadResult> ReadCoreAsync(DocumentReadContext context, CancellationToken cancellationToken)
    {
        var options = context.Options.GetOptions<PdfReadOptions>();

        using PdfDocument document = PdfDocument.Open(context.Stream);

        int totalPages = document.NumberOfPages;
        int maxPages = options.MaxPages ?? context.Options.MaxPages ?? int.MaxValue;
        int pagesToRead = Math.Min(totalPages, maxPages);
        if (pagesToRead < totalPages)
        {
            context.AddWarning(WarningCodes.PdfMaxPagesExceeded,
                $"Only the first {pagesToRead} of {totalPages} pages were read.");
        }

        var pages = new List<DocumentPage>(pagesToRead);
        var tables = new List<DocumentTable>();
        var text = new StringBuilder();
        int pagesWithText = 0;

        for (int pageNumber = 1; pageNumber <= pagesToRead; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Page page = document.GetPage(pageNumber);

            bool hasTextLayer = page.Letters.Count > 0;
            string pageText = hasTextLayer ? ExtractText(page, options) : string.Empty;

            var pageTables = new List<DocumentTable>();
            if (hasTextLayer)
            {
                pagesWithText++;
                text.Append(pageText).Append("\n\n");

                if (options.ExtractTables && PdfTableExtractor.Extract(page, pageNumber) is { } extracted)
                {
                    pageTables.Add(extracted.Table);
                    tables.Add(extracted.Table);
                    if (extracted.Confidence <= options.LowConfidenceThreshold)
                    {
                        context.AddWarning(WarningCodes.PdfLowConfidenceTableExtraction,
                            $"Low-confidence table extraction on page {pageNumber} (confidence {extracted.Confidence:0.00}).",
                            new DocumentLocation(PageNumber: pageNumber));
                    }
                }
            }
            else
            {
                context.AddWarning(WarningCodes.PdfTextLayerMissing,
                    $"Page {pageNumber} has no text layer (likely scanned); OCR would be required.",
                    new DocumentLocation(PageNumber: pageNumber));
            }

            pages.Add(new DocumentPage { PageNumber = pageNumber, Text = pageText, Tables = pageTables });
        }

        var result = new DocumentReadResult
        {
            Source = context.CreateSourceInfo(DocumentKind.Pdf),
            Kind = DocumentKind.Pdf,
            Text = context.Options.ExtractText ? text.ToString().TrimEnd('\n') : null,
            Pages = pages,
            Tables = tables,
            Metadata = ReadMetadata(document, totalPages),
            Quality = AssessQuality(pagesWithText, pages.Count),
            Warnings = context.Warnings.ToArray(),
        };

        return Task.FromResult(result);
    }

    private static string ExtractText(Page page, PdfReadOptions options)
        => options.PreserveLayout ? page.Text : ContentOrderTextExtractor.GetText(page);

    private static ExtractionQuality AssessQuality(int pagesWithText, int pageCount)
    {
        if (pageCount == 0 || pagesWithText == 0)
        {
            return ExtractionQuality.Low;
        }

        return pagesWithText == pageCount ? ExtractionQuality.High : ExtractionQuality.Medium;
    }

    private static DocumentMetadata ReadMetadata(PdfDocument document, int pageCount)
    {
        DocumentInformation information = document.Information;
        return new DocumentMetadata
        {
            Title = string.IsNullOrEmpty(information.Title) ? null : information.Title,
            Author = string.IsNullOrEmpty(information.Author) ? null : information.Author,
            CreatedUtc = PdfDateParser.Parse(information.CreationDate),
            ModifiedUtc = PdfDateParser.Parse(information.ModifiedDate),
            PageCount = pageCount,
        };
    }
}
