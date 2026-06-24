using System.Text;
using DeepSigma.DocumentReader.Core;
using DeepSigma.DocumentReader.Core.Detection;
using DeepSigma.DocumentReader.Html;
using Xunit;

namespace DeepSigma.DocumentReader.Html.Tests;

public sealed class HtmlDocumentReaderTests
{
    private const string Sample = """
        <!DOCTYPE html>
        <html>
        <head><title>Demo Page</title></head>
        <body>
          <h1>Overview</h1>
          <p>Revenue increased <strong>year over year</strong>.</p>
          <h2>Details</h2>
          <table>
            <tr><th>Metric</th><th>Q1</th></tr>
            <tr><td>Revenue</td><td>100</td></tr>
          </table>
          <a href="https://example.com">docs</a>
          <script>console.log('ignored');</script>
        </body>
        </html>
        """;

    private static async Task<DocumentReadResult> ReadAsync(string html = Sample)
    {
        var reader = new HtmlDocumentReader();
        using var source = DocumentSource.FromBytes(Encoding.UTF8.GetBytes(html), "page.html");
        return await reader.ReadAsync(source, DocumentReadOptions.Default);
    }

    [Fact]
    public async Task Extracts_readable_text_excluding_scripts()
    {
        DocumentReadResult result = await ReadAsync();

        Assert.Equal(DocumentKind.Html, result.Kind);
        Assert.Contains("Revenue increased year over year.", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("console.log", result.Text, StringComparison.Ordinal);
        Assert.Equal("Demo Page", result.Metadata.Title);
    }

    [Fact]
    public async Task Builds_sections_tables_and_links()
    {
        DocumentReadResult result = await ReadAsync();

        DocumentSection top = Assert.Single(result.Sections);
        Assert.Equal("Overview", top.Title);
        Assert.Contains("Revenue increased year over year.", top.Text, StringComparison.Ordinal);
        Assert.Equal("Details", Assert.Single(top.Children).Title);

        DocumentTable table = Assert.Single(result.Tables);
        Assert.Equal(["Metric", "Q1"], table.Headers);
        Assert.Equal("Revenue", table.Rows[0].Cells[0].Text);

        var feature = result.GetFeature<HtmlDocumentFeature>();
        Assert.Contains(feature!.Links, l => l.Url == "https://example.com");
    }

    [Fact]
    public async Task Html_text_extractor_is_reusable()
    {
        var extractor = new HtmlTextExtractor();
        string text = extractor.ExtractText("<p>Hello</p><p>World</p>");
        Assert.Equal("Hello\nWorld", text);
    }

    [Fact]
    public async Task Composite_detects_and_routes_html_without_extension()
    {
        var reader = new CompositeDocumentReader([new HtmlDocumentReader()], CompositeDocumentTypeDetector.CreateDefault());
        using var source = DocumentSource.FromBytes(Encoding.UTF8.GetBytes(Sample));

        DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

        Assert.Equal(DocumentKind.Html, result.Kind);
    }
}
