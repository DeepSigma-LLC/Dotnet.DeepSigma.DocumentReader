using System.Text;
using DeepSigma.DocumentReader.Plaintext;
using Xunit;

namespace DeepSigma.DocumentReader.Plaintext.Tests;

public sealed class CsvDocumentReaderTests
{
    private static async Task<DocumentReadResult> ReadAsync(string content, DocumentReadOptions? options = null)
    {
        var reader = new CsvDocumentReader();
        using var source = DocumentSource.FromBytes(Encoding.UTF8.GetBytes(content), "data.csv");
        return await reader.ReadAsync(source, options ?? DocumentReadOptions.Default);
    }

    [Fact]
    public async Task Reads_header_and_rows()
    {
        var result = await ReadAsync("name,age\nAlice,30\nBob,25\n");

        DocumentTable table = Assert.Single(result.Tables);
        Assert.Equal(["name", "age"], table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Alice", table.Rows[0].Cells[0].Text);
        Assert.Equal("25", table.Rows[1].Cells[1].Text);
    }

    [Fact]
    public async Task Detects_semicolon_delimiter()
    {
        var result = await ReadAsync("name;age\nAlice;30\nBob;25\n");

        DocumentTable table = Assert.Single(result.Tables);
        Assert.Equal(["name", "age"], table.Headers);
        Assert.Equal("Alice", table.Rows[0].Cells[0].Text);
    }

    [Fact]
    public async Task Respects_max_rows_with_warning()
    {
        var options = DocumentReadOptions.Default.WithOptions(new CsvReadOptions { MaxRows = 1 });
        var result = await ReadAsync("name,age\nAlice,30\nBob,25\nCarol,42\n", options);

        DocumentTable table = Assert.Single(result.Tables);
        Assert.Single(table.Rows);
        Assert.Contains(result.Warnings, w => w.Code == WarningCodes.CsvMaxRowsExceeded);
    }
}
