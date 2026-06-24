using DeepSigma.DocumentReader.Core;
using DeepSigma.DocumentReader.Core.Detection;
using DeepSigma.DocumentReader.Email;
using Xunit;

namespace DeepSigma.DocumentReader.Email.Tests;

public sealed class OutlookMsgDocumentReaderTests
{
    private static CompositeDocumentReader CreateComposite()
        => new(
            [new EmailDocumentReader(), new OutlookMsgDocumentReader()],
            CompositeDocumentTypeDetector.CreateDefault());

    [Fact]
    public async Task Extracts_fields_body_and_attachment()
    {
        var reader = new OutlookMsgDocumentReader();
        using var source = DocumentSource.FromBytes(MsgSamples.Create(), "message.msg");

        DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

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
    }

    [Fact]
    public async Task Composite_routes_msg_content_to_outlook_reader()
    {
        CompositeDocumentReader reader = CreateComposite();
        using var source = DocumentSource.FromBytes(MsgSamples.Create(), "message.msg");

        DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

        Assert.Equal(DocumentKind.Email, result.Kind);
        Assert.Equal("Quarterly Review", result.GetFeature<EmailDocumentFeature>()!.Subject);
    }

    [Fact]
    public async Task Composite_still_routes_eml_to_mime_reader()
    {
        CompositeDocumentReader reader = CreateComposite();
        using var source = DocumentSource.FromBytes(EmailSamples.MultipartWithAttachment());

        DocumentReadResult result = await reader.ReadAsync(source, DocumentReadOptions.Default);

        Assert.Equal(DocumentKind.Email, result.Kind);
        Assert.Contains("Revenue increased", result.Text, StringComparison.Ordinal);
        Assert.Equal("notes.txt", Assert.Single(result.Attachments).FileName);
    }
}
