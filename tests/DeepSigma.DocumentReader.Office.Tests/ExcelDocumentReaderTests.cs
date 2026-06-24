using DeepSigma.DocumentReader.Office;
using Xunit;

namespace DeepSigma.DocumentReader.Office.Tests;

public sealed class ExcelDocumentReaderTests
{
    private static async Task<DocumentReadResult> ReadAsync()
    {
        var reader = new ExcelDocumentReader();
        using var source = DocumentSource.FromBytes(OfficeSamples.CreateExcel(), "sample.xlsx");
        return await reader.ReadAsync(source, DocumentReadOptions.Default);
    }

    [Fact]
    public async Task Extracts_sheet_cells_with_typed_values()
    {
        DocumentReadResult result = await ReadAsync();

        Assert.Equal(DocumentKind.Spreadsheet, result.Kind);
        var feature = result.GetFeature<SpreadsheetDocumentFeature>();
        Assert.NotNull(feature);
        SpreadsheetSheet sheet = Assert.Single(feature!.Sheets);
        Assert.Equal("Data", sheet.Name);

        DocumentTableCell aliceAge = sheet.Table.Rows[1].Cells[1];
        Assert.Equal(30d, aliceAge.RawValue);
        Assert.Equal("Alice", sheet.Table.Rows[1].Cells[0].Text);
    }

    [Fact]
    public async Task Warns_that_formulas_are_not_recalculated()
    {
        DocumentReadResult result = await ReadAsync();
        Assert.Contains(result.Warnings, w => w.Code == WarningCodes.ExcelFormulaNotCalculated);
    }
}
