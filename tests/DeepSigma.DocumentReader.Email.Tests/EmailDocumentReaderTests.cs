using DeepSigma.DocumentReader.Core;
using DeepSigma.DocumentReader.Core.Detection;
using DeepSigma.DocumentReader.Email;
using Xunit;

namespace DeepSigma.DocumentReader.Email.Tests;

public sealed class EmailDocumentReaderTests
{
    private sealed class FakeHtmlExtractor : IHtmlTextExtractor
    {
        public string ExtractText(string html) => "EXTRACTED-BY-HTML-READER";
    }

    private static async Task<DocumentReadResult> ReadAsync(byte[] eml, EmailDocumentReader? reader = null)
    {
        reader ??= new EmailDocumentReader();
        using var source = DocumentSource.FromBytes(eml, "message.eml");
        return await reader.ReadAsync(source, DocumentReadOptions.Default);
    }

    [Fact]
    public async Task Extracts_headers_body_and_attachment()
    {
        DocumentReadResult result = await ReadAsync(EmailSamples.MultipartWithAttachment());

        Assert.Equal(DocumentKind.Email, result.Kind);
        Assert.Contains("Revenue increased", result.Text, StringComparison.Ordinal);

        var feature = result.GetFeature<EmailDocumentFeature>();
        Assert.NotNull(feature);
        Assert.Equal("Quarterly Review", feature!.Subject);
        Assert.Equal("alice@example.com", Assert.Single(feature.From).Address);
        Assert.Equal("bob@example.com", Assert.Single(feature.To).Address);
        Assert.Equal("carol@example.com", Assert.Single(feature.Cc).Address);

        DocumentAttachment attachment = Assert.Single(result.Attachments);
        Assert.Equal("notes.txt", attachment.FileName);
        using var stream = attachment.OpenReadStream();
        using var streamReader = new StreamReader(stream);
        Assert.Contains("attached note body", await streamReader.ReadToEndAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Uses_registered_html_text_extractor_for_html_body()
    {
        var reader = new EmailDocumentReader([new FakeHtmlExtractor()]);
        DocumentReadResult result = await ReadAsync(EmailSamples.HtmlBodyOnly(), reader);

        Assert.Equal("EXTRACTED-BY-HTML-READER", result.Text);
    }

    [Fact]
    public async Task Falls_back_to_naive_strip_without_html_extractor()
    {
        DocumentReadResult result = await ReadAsync(EmailSamples.HtmlBodyOnly());

        Assert.Contains("Hello HTML world.", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("<p>", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preserves_repeated_headers()
    {
        DocumentReadResult result = await ReadAsync(EmailSamples.RepeatedHeaders());

        var feature = result.GetFeature<EmailDocumentFeature>();
        string received = Assert.Contains("Received", (IReadOnlyDictionary<string, string>)feature!.Headers);
        Assert.Contains("a.example.com", received, StringComparison.Ordinal);
        Assert.Contains("b.example.com", received, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Composite_detects_and_routes_eml_by_content()
    {
        var reader = new CompositeDocumentReader([new EmailDocumentReader()], CompositeDocumentTypeDetector.CreateDefault());
        using var source = DocumentSource.FromBytes(EmailSamples.MultipartWithAttachment());

        DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

        Assert.Equal(DocumentKind.Email, result.Kind);
    }
}
