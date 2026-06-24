using DeepSigma.DocumentReader.Core;
using DeepSigma.DocumentReader.Core.Detection;
using DeepSigma.DocumentReader.Office;
using Xunit;

namespace DeepSigma.DocumentReader.Office.Tests;

public sealed class OfficeRoutingTests
{
    private static CompositeDocumentReader CreateReader()
        => new(
            [new WordDocumentReader(), new ExcelDocumentReader(), new PowerPointDocumentReader()],
            CompositeDocumentTypeDetector.CreateDefault());

    [Fact]
    public async Task Composite_routes_docx_to_word_reader()
    {
        var reader = CreateReader();
        using var source = DocumentSource.FromBytes(OfficeSamples.CreateWord(), "report.docx");

        DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

        Assert.Equal(DocumentKind.WordDocument, result.Kind);
        Assert.Contains("Revenue increased", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Composite_routes_xlsx_to_excel_reader()
    {
        var reader = CreateReader();
        using var source = DocumentSource.FromBytes(OfficeSamples.CreateExcel(), "data.xlsx");

        DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

        Assert.Equal(DocumentKind.Spreadsheet, result.Kind);
    }
}
