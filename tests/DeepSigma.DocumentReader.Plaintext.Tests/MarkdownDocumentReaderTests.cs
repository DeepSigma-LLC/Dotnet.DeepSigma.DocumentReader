using System.Text;
using DeepSigma.DocumentReader.Plaintext;
using Xunit;

namespace DeepSigma.DocumentReader.Plaintext.Tests;

public sealed class MarkdownDocumentReaderTests
{
    private const string Sample = """
        ---
        title: Demo
        author: Jane
        ---

        # Top

        Intro text.

        ## Sub

        | A | B |
        |---|---|
        | 1 | 2 |

        ```csharp
        var x = 1;
        ```

        See [docs](https://example.com).
        """;

    private static async Task<DocumentReadResult> ReadAsync(string content)
    {
        var reader = new MarkdownDocumentReader();
        using var source = DocumentSource.FromBytes(Encoding.UTF8.GetBytes(content), "doc.md");
        return await reader.ReadAsync(source, DocumentReadOptions.Default);
    }

    [Fact]
    public async Task Builds_section_tree_from_headings()
    {
        var result = await ReadAsync(Sample);

        DocumentSection top = Assert.Single(result.Sections);
        Assert.Equal("Top", top.Title);
        DocumentSection sub = Assert.Single(top.Children);
        Assert.Equal("Sub", sub.Title);
        Assert.Equal("/Top/Sub", sub.Location!.SectionPath);
    }

    [Fact]
    public async Task Extracts_table()
    {
        var result = await ReadAsync(Sample);

        DocumentTable table = Assert.Single(result.Tables);
        Assert.Equal(["A", "B"], table.Headers);
        Assert.Equal("1", table.Rows[0].Cells[0].Text);
    }

    [Fact]
    public async Task Extracts_front_matter_code_blocks_and_links()
    {
        var result = await ReadAsync(Sample);

        var feature = result.GetFeature<MarkdownDocumentFeature>();
        Assert.NotNull(feature);
        Assert.Equal("Demo", feature!.FrontMatter["title"]);
        Assert.Contains(feature.CodeBlocks, c => c.Language == "csharp");
        Assert.DoesNotContain(feature.CodeBlocks, c => c.Code.Contains("title:", StringComparison.Ordinal));
        Assert.Contains(feature.Links, l => l.Url == "https://example.com");
    }
}
