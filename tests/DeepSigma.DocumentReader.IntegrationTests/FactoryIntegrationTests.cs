using DeepSigma.DocumentReader;
using Xunit;

namespace DeepSigma.DocumentReader.IntegrationTests;

public sealed class FactoryIntegrationTests
{
    private static readonly IDocumentReader Reader = DocumentReaderFactory.CreateDefault();

    [Theory]
    [InlineData("Text/sample.txt", DocumentKind.PlainText)]
    [InlineData("Markdown/sample.md", DocumentKind.Markdown)]
    [InlineData("Json/sample.json", DocumentKind.Json)]
    [InlineData("Json/sample.jsonl", DocumentKind.JsonLines)]
    [InlineData("Csv/sample.csv", DocumentKind.Csv)]
    public async Task Reads_each_corpus_file_to_the_expected_kind(string relativePath, DocumentKind expectedKind)
    {
        string path = TestPaths.Corpus(relativePath.Split('/'));
        using var source = DocumentSource.FromFile(path);

        DocumentReadResult result = await Reader.ReadAsync(source, DocumentReadOptions.Default);

        Assert.Equal(expectedKind, result.Kind);
        Assert.False(string.IsNullOrEmpty(result.Text));
    }

    [Fact]
    public async Task Markdown_round_trips_structure()
    {
        string path = TestPaths.Corpus("Markdown", "sample.md");
        using var source = DocumentSource.FromFile(path);

        DocumentReadResult result = await Reader.ReadAsync(source, DocumentReadOptions.Default);

        Assert.NotEmpty(result.Sections);
        Assert.NotEmpty(result.Tables);
    }
}
