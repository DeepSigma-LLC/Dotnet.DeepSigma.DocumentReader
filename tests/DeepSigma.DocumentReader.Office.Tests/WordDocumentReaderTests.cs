using DeepSigma.DocumentReader.Office;
using Xunit;

namespace DeepSigma.DocumentReader.Office.Tests;

public sealed class WordDocumentReaderTests
{
    private static async Task<DocumentReadResult> ReadAsync()
    {
        var reader = new WordDocumentReader();
        using var source = DocumentSource.FromBytes(OfficeSamples.CreateWord(), "sample.docx");
        return await reader.ReadAsync(source, DocumentReadOptions.Default);
    }

    [Fact]
    public async Task Extracts_text_headings_and_tables()
    {
        DocumentReadResult result = await ReadAsync();

        Assert.Equal(DocumentKind.WordDocument, result.Kind);
        Assert.Contains("Revenue increased", result.Text, StringComparison.Ordinal);

        DocumentSection top = Assert.Single(result.Sections);
        Assert.Equal("Overview", top.Title);
        Assert.Equal("Details", Assert.Single(top.Children).Title);

        DocumentTable table = Assert.Single(result.Tables);
        Assert.Equal("100", table.Rows[1].Cells[1].Text);
    }
}
