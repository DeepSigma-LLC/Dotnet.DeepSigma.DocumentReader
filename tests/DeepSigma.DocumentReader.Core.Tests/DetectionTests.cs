using System.Text;
using DeepSigma.DocumentReader.Core.Detection;
using Xunit;

namespace DeepSigma.DocumentReader.Core.Tests;

public sealed class DetectionTests
{
    private static readonly CompositeDocumentTypeDetector Detector = CompositeDocumentTypeDetector.CreateDefault();

    private static async Task<DocumentTypeDetectionResult> DetectAsync(string content, string? fileName = null, string? contentType = null)
    {
        using var source = DocumentSource.FromBytes(Encoding.UTF8.GetBytes(content), fileName, contentType);
        return await Detector.DetectAsync(source);
    }

    [Fact]
    public async Task Detects_json_by_extension()
    {
        var result = await DetectAsync("{\"a\":1}", "data.json");
        Assert.Equal(DocumentKind.Json, result.Kind);
    }

    [Fact]
    public async Task Detects_csv_by_content_without_filename()
    {
        var result = await DetectAsync("a,b,c\n1,2,3\n4,5,6\n");
        Assert.Equal(DocumentKind.Csv, result.Kind);
    }

    [Fact]
    public async Task Detects_markdown_by_heading()
    {
        var result = await DetectAsync("# Title\n\nSome prose here.\n");
        Assert.Equal(DocumentKind.Markdown, result.Kind);
    }

    [Fact]
    public async Task Detects_json_lines_by_content()
    {
        var result = await DetectAsync("{\"a\":1}\n{\"a\":2}\n{\"a\":3}\n");
        Assert.Equal(DocumentKind.JsonLines, result.Kind);
    }

    [Fact]
    public async Task Falls_back_to_plain_text()
    {
        var result = await DetectAsync("just some words with no structure at all");
        Assert.Equal(DocumentKind.PlainText, result.Kind);
    }

    [Fact]
    public async Task Explicit_content_type_wins_over_extension()
    {
        var result = await DetectAsync("plain", "data.txt", contentType: "application/json");
        Assert.Equal(DocumentKind.Json, result.Kind);
    }

    [Fact]
    public async Task Pdf_detected_by_magic_bytes()
    {
        var result = await DetectAsync("%PDF-1.7\n...", fileName: null);
        Assert.Equal(DocumentKind.Pdf, result.Kind);
    }
}
