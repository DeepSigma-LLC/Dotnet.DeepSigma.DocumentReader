using DeepSigma.DocumentReader.Core;
using DeepSigma.DocumentReader.Core.Detection;
using DeepSigma.DocumentReader.Pdf;
using Xunit;

namespace DeepSigma.DocumentReader.Pdf.Tests;

public sealed class PdfDocumentReaderTests
{
    private static async Task<DocumentReadResult> ReadAsync(byte[] bytes, DocumentReadOptions? options = null)
    {
        var reader = new PdfDocumentReader();
        using var source = DocumentSource.FromBytes(bytes, "sample.pdf");
        return await reader.ReadAsync(source, options ?? DocumentReadOptions.Default);
    }

    [Fact]
    public async Task Extracts_page_text_and_page_model()
    {
        DocumentReadResult result = await ReadAsync(PdfSamples.CreateWithText(2));

        Assert.Equal(DocumentKind.Pdf, result.Kind);
        Assert.Equal(2, result.Pages.Count);
        Assert.Contains("Hello from page 1", result.Text, StringComparison.Ordinal);
        Assert.Contains("Hello from page 2", result.Text, StringComparison.Ordinal);
        Assert.Equal(ExtractionQuality.High, result.Quality);
        Assert.Equal(2, result.Metadata.PageCount);
    }

    [Fact]
    public async Task Warns_and_degrades_when_page_has_no_text_layer()
    {
        DocumentReadResult result = await ReadAsync(PdfSamples.CreateWithoutText());

        Assert.Contains(result.Warnings, w => w.Code == WarningCodes.PdfTextLayerMissing);
        Assert.Equal(ExtractionQuality.Low, result.Quality);
    }

    [Fact]
    public async Task Respects_max_pages_with_warning()
    {
        var options = DocumentReadOptions.Default.WithOptions(new PdfReadOptions { MaxPages = 1 });
        DocumentReadResult result = await ReadAsync(PdfSamples.CreateWithText(3), options);

        Assert.Single(result.Pages);
        Assert.Contains(result.Warnings, w => w.Code == WarningCodes.PdfMaxPagesExceeded);
    }

    [Fact]
    public async Task Extracts_grid_table_when_enabled()
    {
        var options = DocumentReadOptions.Default.WithOptions(new PdfReadOptions { ExtractTables = true });
        DocumentReadResult result = await ReadAsync(PdfSamples.CreateGrid(), options);

        DocumentTable table = Assert.Single(result.Tables);
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal(3, table.Rows[0].Cells.Count);
        Assert.Equal("Name", table.Rows[0].Cells[0].Text);
        Assert.Equal("Alice", table.Rows[1].Cells[0].Text);
        Assert.Equal("London", table.Rows[1].Cells[2].Text);
        Assert.NotNull(table.Confidence);
        Assert.True(table.Confidence > 0.9, $"confidence was {table.Confidence}");
    }

    [Fact]
    public async Task Does_not_extract_tables_by_default()
    {
        DocumentReadResult result = await ReadAsync(PdfSamples.CreateGrid());
        Assert.Empty(result.Tables);
    }

    [Fact]
    public async Task Composite_detects_and_routes_pdf()
    {
        var reader = new CompositeDocumentReader([new PdfDocumentReader()], CompositeDocumentTypeDetector.CreateDefault());
        using var source = DocumentSource.FromBytes(PdfSamples.CreateWithText(), "report.pdf");

        DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

        Assert.Equal(DocumentKind.Pdf, result.Kind);
    }
}
